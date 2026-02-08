import {Component, inject, OnInit, resource} from '@angular/core';
import {TableModule} from "primeng/table";
import {Site} from "@domain/Site";
import {firstValueFrom} from "rxjs";
import {JsonPipe} from "@angular/common";
import {RouterLink} from "@angular/router";
import {Button} from "primeng/button";
import {AppContextService} from "@services/app-context.service";


@Component({
  selector: 'app-sites',
    imports: [
        TableModule,
        JsonPipe,
        RouterLink,
        Button
    ],
  templateUrl: './sites.component.html',
  styleUrl: './sites.component.scss'
})
export class SitesComponent implements OnInit {
    selected: Site[] = []

    #appContext = inject(AppContextService);   
    
    ngOnInit(): void {
        this.sites.reload();
    }
    
    get sites() { return this.#appContext.sites; }
}
