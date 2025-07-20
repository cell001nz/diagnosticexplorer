import {Component, computed, inject, input, resource, signal} from '@angular/core';
import {InputText} from "primeng/inputtext";
import {Fluid} from "primeng/fluid";
import {Site} from "../model/Site";
import {DataService} from "../services/data.service";
import {firstValueFrom, of} from "rxjs";
import {rxResource} from "@angular/core/rxjs-interop";
import {FormsModule} from "@angular/forms";
import {Button} from "primeng/button";
import {ActivatedRoute, Router} from "@angular/router";
import {getErrorMsg} from "../../util/errorUtil";
import {MessageService} from "primeng/api";
import {Toast} from "primeng/toast";
import {JsonPipe} from "@angular/common";
import {Secret} from "../model/Secret";

@Component({
  selector: 'app-edit-site',
  imports: [
    InputText,
    Fluid,
    FormsModule,
    Button,
    JsonPipe
  ],
  templateUrl: './edit-site.component.html',
  styleUrl: './edit-site.component.scss',
})
export class EditSiteComponent {

  id = input('');
  editMode = input(false, { transform: x => !!x});
  isBusy = signal(false);
  saveError = signal('');
  #data = inject(DataService);
  #router = inject(Router);
  #route = inject(ActivatedRoute);
  messageService = inject(MessageService);
  
  
  title = computed(() => !!this.id() ? 'Edit Site' : 'Create New Site');
  
  #createNew(): Site {
    return {
      id: '',
      code: crypto.randomUUID(),
      name: 'New Site',
      secrets: []
    }
  }
  
  site = rxResource({
    defaultValue: { } as Site,
    params: () => ({id: this.id()}),
    stream: ({params: p}) => p.id 
        ? this.#data.getSite(p.id) 
        : of(this.#createNew()) 
  })

  async save() {
    this.saveError.set('');    
    this.isBusy.set(true);
    
    try {
      let toSave = this.site.value()!;
      let result = await firstValueFrom(this.#data.saveSite(toSave))
      this.messageService.add({severity: 'success', summary: 'Saved', detail: 'Site saved', life: 1000});
      
      if (!toSave.id)
        await this.#router.navigate(['..', result.id], {relativeTo: this.#route});
      else
        this.site.reload();    
    }
    catch (err) {
      this.saveError.set(getErrorMsg(err));     
    }
    finally {
      this.isBusy.set(false);
    }
    
  }

  addSecret() {
    this.#data.newSecret()
        .subscribe(secret => {
          this.site.value().secrets.push( {id: '',  name: "New Secret", value: secret})
        })
  }

  removeSecret(secret: Secret) {
    this.site.value().secrets = this.site.value().secrets.filter(s => s !== secret); 
  }
}
