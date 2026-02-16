import {Component, computed, inject, input, resource, signal} from '@angular/core';
import {InputText} from "primeng/inputtext";
import {Fluid} from "primeng/fluid";
import {Site} from "@domain/Site";
import {firstValueFrom, of} from "rxjs";
import {rxResource} from "@angular/core/rxjs-interop";
import {FormsModule} from "@angular/forms";
import {Button} from "primeng/button";
import {ActivatedRoute, Router} from "@angular/router";
import {getErrorMsg} from "@util/errorUtil";
import {MessageService} from "primeng/api";
import {Secret} from "@domain/Secret";
import {SiteIOService} from "@services/siteIO.service";
import {AppContextService} from "@services/app-context.service";

@Component({
  selector: 'app-edit-site',
  imports: [
    InputText,
    Fluid,
    FormsModule,
    Button
  ],
  templateUrl: './edit-site.component.html',
  styleUrl: './edit-site.component.scss',
})
export class EditSiteComponent {

  id = input('');
  createNew = input(false, { transform: x => !!x});
  isBusy = signal(false);
  saveError = signal('');
  #siteService = inject(SiteIOService);
  #appContext = inject(AppContextService);
  #router = inject(Router);
  #route = inject(ActivatedRoute);
  messageService = inject(MessageService);  
  
  title = computed(() => !!this.id() ? 'Edit Site' : 'Create New Site');
  
  #createNew(): Site {
    return {
      id: 0,
      name: 'New Site',
      code: crypto.randomUUID(),
      secrets: []
    }
  }
  
  site = rxResource({
    defaultValue: { } as Site,
    params: () => ({id: this.id()}),
    stream: ({params: p}) => p.id 
        ? this.#siteService.getSite(p.id) 
        : of(this.#createNew()) 
  })

  async save() {
    this.saveError.set('');    
    this.isBusy.set(true);
    
    try {
      let toSave = this.site.value()!;
      
      let result = this.createNew()
          ? await firstValueFrom(this.#siteService.insertSite(toSave))
          : await firstValueFrom(this.#siteService.updateSite(toSave));
          
      this.messageService.add({severity: 'success', summary: 'Saved', detail: 'Site saved', life: 1000});
      
      if (!this.id())
        await this.#router.navigate(['..', result.id], {relativeTo: this.#route});
      else
        this.site.reload();    
    }
    catch (err) {
      this.saveError.set(getErrorMsg(err));     
    }
    finally {
      this.isBusy.set(false);
      this.#appContext.sites.reload();
    }    
  }

  addSecret() {
    this.#siteService.newSecret()
        .subscribe(secret => {
          this.site.value().secrets.push( {id: 0,  name: "New Secret", value: secret})
        })
  }

  removeSecret(secret: Secret) {
    this.site.value().secrets = this.site.value().secrets.filter(s => s !== secret); 
  }
}
