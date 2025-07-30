import {Component, inject, input, OnDestroy, OnInit, signal} from '@angular/core';
import {
  catchError,
  combineLatest,
  combineLatestWith,
  concat,
  concatAll,
  concatMap,
  delay, fromEvent, interval,
  map,
  of, repeat,
  startWith,
  switchMap, take,
  takeUntil,
  tap,
  timer
} from "rxjs";
import {rxResource, takeUntilDestroyed, toObservable, toSignal} from "@angular/core/rxjs-interop";
import {SiteService} from "@services/site.service";
import {JsonPipe} from "@angular/common";
import {ActivatedRoute} from "@angular/router";
import {SassWorkerImplementation} from "@angular/build/private";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {StepperSeparator} from "primeng/stepper";
import {Divider} from "primeng/divider";

@Component({
  selector: 'app-diagnostics-view',
  imports: [
    JsonPipe,
    ErrMsgPipe,
    StepperSeparator,
    Divider
  ],
  templateUrl: './diagnostics-view.component.html',
  styleUrl: './diagnostics-view.component.scss'
})
export class DiagnosticsViewComponent {

  processId = input.required<string>();
  #sitesService = inject(SiteService);
  diags = signal<{ result?: string, error?: any, loading?: boolean }>({})
  route = inject(ActivatedRoute);
  
  get siteId() {
    return this.route.parent?.snapshot.params['siteId'];
  } 
    
  constructor() {
    toObservable(this.processId)
        .pipe(
            switchMap(processId =>
              concat(
                  of({ loading: true}),    
                  this.#sitesService.getDiagnostics(this.siteId, processId)
                      .pipe(
                          map(result => ({result})),
                          catchError(error => of({ error })),
                          repeat({delay: 1000})
                      )
              )
            )
        ).subscribe(diags => this.diags.set(diags))
  }
  
}
