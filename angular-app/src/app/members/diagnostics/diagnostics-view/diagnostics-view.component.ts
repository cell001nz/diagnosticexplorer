import {ChangeDetectionStrategy, Component, computed, effect, inject, input, OnDestroy, OnInit, signal} from '@angular/core';
import {
  catchError,
  combineLatest,
  combineLatestWith,
  concat,
  concatAll,
  concatMap,
  delay, filter, finalize, fromEvent, interval,
  map,
  of, repeat, retry,
  startWith,
  switchMap, take,
  takeUntil,
  tap,
  timer
} from "rxjs";
import {rxResource, takeUntilDestroyed, toObservable, toSignal} from "@angular/core/rxjs-interop";
import {SiteService} from "@services/site.service";
import {DatePipe, DecimalPipe, JsonPipe} from "@angular/common";
import {ActivatedRoute} from "@angular/router";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {DiagHubService} from "@services/diag-hub.service";
import {DiagnosticModelFactory} from "@model/DiagnosticModelFactory";
import {RealtimeModel} from "@model/RealtimeModel";
import {Divider} from "primeng/divider";
import {DiagnosticResponse} from "@domain/DiagResponse";
import {DateDiffPipe} from "@pipes/date-diff.pipe";
import {DefValPipe} from "@pipes/def-val.pipe";
import {NumberFractionPipe} from "@pipes/number-fraction.pipe";
import {Tab, TabList, TabPanel, TabPanels, Tabs} from "primeng/tabs";

const REFRESH_INTERVAL = 5_000;

@Component({
  selector: 'app-diagnostics-view',
  imports: [
    JsonPipe,
    ErrMsgPipe,
    DecimalPipe,
    NumberFractionPipe,
    Tabs,
    TabPanel,
    TabList,
    Tab,
    TabPanels,
  ],
  templateUrl: './diagnostics-view.component.html',
  styleUrl: './diagnostics-view.component.scss',
  providers: [DiagnosticModelFactory, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiagnosticsViewComponent {
  route = inject(ActivatedRoute);
  processId = input.required<string>();  
  #sitesService = inject(SiteService);
  #hubService = inject(DiagHubService); 
  modelFactory = inject(DiagnosticModelFactory);
  realtime = inject(RealtimeModel)
  isLoading = signal(false);
  lastUpdated = signal(Date.now());
  nextRefresh = signal<number | undefined>(undefined); 

  constructor() {
    toObservable(this.diags.value)
        .pipe(filter(d => !!d))
        .subscribe(d => this.realtime.update(d));
    
    effect(() => console.log('status', this.diags.status()));
    
    timer(0, 100).subscribe(() => {
      this.nextRefresh.set(this.isLoading() 
          ? undefined
          : (REFRESH_INTERVAL - (Date.now() - this.lastUpdated())) / 1000)
    });
  }
  
  get siteId() {
    return this.route.parent?.snapshot.params['siteId'];
  } 
  
  diags = rxResource({
    params: () => ({ processId: this.processId() }),
    stream: ({params: p}) => 
        concat(
            of(null).pipe(tap(() => this.setIsLoading(true)), filter(x => false)),
            this.#sitesService.getDiagnostics(this.siteId, p.processId),
        ).pipe(
            finalize(() => this.setIsLoading(false)),
            tap({error: err => console.log('error', err)}),
            retry({ delay: REFRESH_INTERVAL, resetOnSuccess: true }),
            repeat({delay: REFRESH_INTERVAL})
        )
    }
  );
  
  setIsLoading(isLoading: boolean) {
    this.isLoading.set(isLoading);
    if (!isLoading)
      this.lastUpdated.set(Date.now());
  }
}
