import {Component, inject, input, OnDestroy} from '@angular/core';
import {rxResource, takeUntilDestroyed, toObservable} from "@angular/core/rxjs-interop";
import {TableModule} from "primeng/table";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {RouterLink} from "@angular/router";
import {DateDiffPipe} from "@pipes/date-diff.pipe";
import {DiagHubService} from "@services/diag-hub.service";
import {filter, pairwise, startWith, tap} from 'rxjs';
import { DiagProcess } from '@domain/DiagProcess';
import {Tooltip} from "primeng/tooltip";
import {SiteIOService} from "@services/siteIO.service";
import {AppContextService} from "@services/app-context.service";

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
export class DiagnosticsNavComponent {
  #appContext = inject(AppContextService);

  get processes() { return this.#appContext.processes; }
  
}
