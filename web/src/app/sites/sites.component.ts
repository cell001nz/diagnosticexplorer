import {Component, inject, resource} from '@angular/core';
import {TableModule} from "primeng/table";
import {Site} from "../model/site";
import {DataService} from "../services/data.service";
import {firstValueFrom} from "rxjs";
import {JsonPipe} from "@angular/common";
import {RouterLink} from "@angular/router";
import {Button} from "primeng/button";
import {rxResource} from "@angular/core/rxjs-interop";

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
export class SitesComponent {

    selected: Site[] = []
    #data = inject(DataService);
    
    sites = rxResource({
        defaultValue: [],
        stream: () => this.#data.getSites()
    })  
}
