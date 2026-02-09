import {ChangeDetectionStrategy, Component, computed, effect, inject, input, OnDestroy, Signal, signal} from '@angular/core';
import {
  concat,
  filter, finalize, map, of, pairwise, repeat, retry, startWith,
  tap,
  timer
} from "rxjs";
import {rxResource, takeUntilDestroyed, toObservable} from "@angular/core/rxjs-interop";
import {DecimalPipe, JsonPipe} from "@angular/common";
import {ActivatedRoute} from "@angular/router";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {DiagHubService} from "@services/diag-hub.service";
import {DiagnosticModelFactory} from "@model/DiagnosticModelFactory";
import {RealtimeModel} from "@model/RealtimeModel";
import {Tab, TabList, TabPanel, TabPanels, Tabs} from "primeng/tabs";
import {CategoryViewComponent} from "@app/members/diagnostics/category-view/category-view.component";
import {SiteIOService} from "@services/siteIO.service";
import {AppContextService} from "@services/app-context.service";
import {DiagProcess} from "@domain/DiagProcess";

const REFRESH_INTERVAL = 5_000;

@Component({
  selector: 'app-diagnostics-view',
  imports: [
    Tabs,
    TabPanel,
    TabList,
    Tab,
    TabPanels,
    CategoryViewComponent,
  ],
  templateUrl: './diagnostics-view.component.html',
  styleUrl: './diagnostics-view.component.scss',
  providers: [DiagnosticModelFactory, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiagnosticsViewComponent implements OnDestroy {
  route = inject(ActivatedRoute);
  processId = input.required<string>();
  #siteIO = inject(SiteIOService);
  #hubService = inject(DiagHubService);
  #appContext = inject(AppContextService);
  realtime = inject(RealtimeModel)
  nextRefresh = signal<number | undefined>(undefined);
  process = computed(() => this.#appContext.processes.value().find(p => p.id === this.processId()));

  constructor() {
    toObservable(this.process)
        .pipe(
            filter(p => !!p),
            takeUntilDestroyed(),
            startWith(undefined),
            pairwise(),
            tap(x => this.realtime.clear()),
            )
        .subscribe(async ([prev, curr]) => {
          await this.tryUnsubscribe(prev)
          await this.trySubscribe(curr)
        })
        // .subscribe(processId => this.#appContext.processId.set(processId));
    
    this.#hubService.clearEvents$
      .pipe(filter(d => d.processId === this.processId()), takeUntilDestroyed())
      .subscribe(d => this.realtime.clearEvents());
    
    this.#appContext.diags$
        .pipe(filter(d => d.processId === this.processId()))
        .subscribe(d => this.realtime.update(d.response));
    
    timer(0, 100).subscribe(() => {
      this.nextRefresh.set(this.#appContext.diagnosticsLoading() 
          ? undefined
          : (REFRESH_INTERVAL - (Date.now() - this.#appContext.diagnosticsUpdated())) / 1000)
    });
  }
  
  site = computed(() => this.#appContext.site());
  
  
  private async tryUnsubscribe(process: DiagProcess | undefined) {
    try {
      if (process)
        await this.#hubService.unsubscribeProcess(process.id, process.siteId);
    } catch (err) {
      console.log(err);
    }
  }
  
  private async trySubscribe(process: DiagProcess | undefined) {
        try {
      if (process)
        await this.#hubService.subscribeProcess(process.id, process.siteId);
    } catch (err) {
      console.log(err);
    }
  }
  
  async ngOnDestroy() {
    await this.tryUnsubscribe(this.process());
  }
  
  expandCollapse() {
    this.realtime.activeCat()?.expandCollapse();
  }

  protected readonly console = console;
  protected readonly String = String;
}
