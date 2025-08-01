import {Component, computed, inject, input, OnDestroy} from '@angular/core';
import {rxResource, takeUntilDestroyed, toObservable} from "@angular/core/rxjs-interop";
import {TableModule} from "primeng/table";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {SiteService} from "@services/site.service";
import {DateDiffPipe} from "@pipes/date-diff.pipe";
import {DiagHubService} from "@services/diag-hub.service";
import {filter, of, pairwise, startWith, tap} from 'rxjs';
import { DiagProcess } from '@domain/DiagProcess';
import {Tooltip} from "primeng/tooltip";

@Component({
  selector: 'app-diagnostics-nav',
  imports: [
    TableModule,
    ErrMsgPipe,
    RouterLink,
    DateDiffPipe,
    Tooltip
  ],
  templateUrl: './diagnostics-nav.component.html',
  styleUrl: './diagnostics-nav.component.scss'
})
export class DiagnosticsNavComponent implements OnDestroy {
  siteId = input.required<string>();
  #hubService = inject(DiagHubService); 
  #sitesService = inject(SiteService);
  
  constructor() {
    this.#hubService.$processArrived.pipe(
        takeUntilDestroyed(),
        tap(p => console.log('process arrived', p, p.siteId, this.siteId())),
        filter(p => p.siteId === this.siteId()),
        filter(p => this.processes.hasValue())
    ).subscribe(process => this.updateProcesses(process));

    
    toObservable(this.siteId)
    .pipe(
        startWith(''),
        pairwise()
    )
    .subscribe(async ([oldSiteId, newSiteId]) => {
        if (oldSiteId)
            await this.#hubService.subscribeSite(oldSiteId)
        if (newSiteId)
            await this.#hubService.subscribeSite(newSiteId)
    });
  }

  updateProcesses(process: DiagProcess): void {
    let matched = this.processes.value().find(p => p.id === process.id);
    if (matched)
      Object.assign(matched, process);
    else
      this.processes.value().push(process);
  }
  
  async ngOnDestroy() {
    if (this.siteId())
      await this.#hubService.unsubscribeSite(this.siteId());
  }  
 
  processes = rxResource({
    defaultValue: [],
    params: () => ({siteId: this.siteId()}),
    stream: ({params: p}) => this.#sitesService.getProcesses(p.siteId)
  })
  
}
