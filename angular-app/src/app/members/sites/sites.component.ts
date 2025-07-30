import {Component, inject, OnInit, resource} from '@angular/core';
import {TableModule} from "primeng/table";
import {Site} from "@model/Site";
import {firstValueFrom} from "rxjs";
import {JsonPipe} from "@angular/common";
import {RouterLink} from "@angular/router";
import {Button} from "primeng/button";
import {rxResource, toObservable} from "@angular/core/rxjs-interop";
import {SiteService} from "@services/site.service";

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

    #data = inject(SiteService);
    
    
    ngOnInit(): void {
        this.sites.reload();
    }
    
    get sites() { return this.#data.sites; }
}
