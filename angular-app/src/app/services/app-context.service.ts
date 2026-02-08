import {computed, inject, Injectable, signal} from '@angular/core';
import {rxResource, takeUntilDestroyed, toObservable} from "@angular/core/rxjs-interop";
import {SiteIOService} from "@services/siteIO.service";
import {concat, filter, finalize, of, pairwise, repeat, retry, startWith, Subject, tap} from "rxjs";
import {DiagProcess} from "@domain/DiagProcess";
import {DiagHubService} from "@services/diag-hub.service";
import {ActivatedRoute} from "@angular/router";
import {DiagnosticResponse} from "@domain/DiagResponse";

const REFRESH_INTERVAL = 5_000;

@Injectable({
  providedIn: 'root'
})
export class AppContextService {
  
  #siteIO = inject(SiteIOService);
  #hubService = inject(DiagHubService);
  
  siteId = signal<string>('');
  site = computed(() => this.sites.value().find(s => s.id === this.siteId()));   
  route = inject(ActivatedRoute);
  
  diags$ = new Subject<{processId: string, response: DiagnosticResponse}>();

  constructor() {
      this.#hubService.processArrived$.pipe(
        takeUntilDestroyed(),
        tap(p => console.log('process arrived', p, p.siteId, this.siteId())),
        filter(p => p.siteId === this.siteId()),
        filter(p => this.processes.hasValue())
    ).subscribe(process => this.updateProcesses(process));
    
      this.#hubService.diagsArrived$.pipe(
        takeUntilDestroyed(),
    ).subscribe(diags => this.diags$.next(diags));
    
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
  
   sites = rxResource({
    defaultValue: [],
    stream: () => this.#siteIO.getSites()
  });
  
  processes = rxResource({
    defaultValue: [],
    params: () => ({siteId: this.siteId()}),
    stream: ({params: p}) => p.siteId ? this.#siteIO.getProcesses(p.siteId) : of([])
  })
  
  
  updateProcesses(process: DiagProcess): void {
    let matched = this.processes.value().find(p => p.id === process.id);
    if (matched)
      Object.assign(matched, process);
    else
      this.processes.value().push(process);
  }
  
  diagnosticsLoading = signal(false);
  diagnosticsUpdated = signal(Date.now());
    
  setIsLoading(isLoading: boolean) {
    this.diagnosticsLoading.set(isLoading);
    if (!isLoading)
      this.diagnosticsUpdated.set(Date.now());
  }
  
  
/*  diags = rxResource({
    params: () => this.process(),
    stream: ({params: process}) => 
        concat(
            of(null).pipe(tap(() => this.setIsLoading(true)), filter(x => false)),
            this.#siteIO.getDiagnostics(process.siteId, process.id)
        ).pipe(
            finalize(() => this.setIsLoading(false)),
            tap({error: err => console.log('error', err)}),
            retry({ delay: REFRESH_INTERVAL, resetOnSuccess: true }),
            repeat({delay: REFRESH_INTERVAL})
        )
    }
  );*/  
}
