import {Component, inject, input, OnDestroy} from '@angular/core';
import {Splitter} from "primeng/splitter";
import {DiagnosticsNavComponent} from "@app/members/diagnostics/diagnostics-nav/diagnostics-nav.component";
import {RouterOutlet} from "@angular/router";
import {Divider} from "primeng/divider";
import {DiagnosticsViewComponent} from "@app/members/diagnostics/diagnostics-view/diagnostics-view.component";
import {toObservable} from "@angular/core/rxjs-interop";
import {AppContextService} from "@services/app-context.service";

@Component({
  selector: 'app-diagnostics-main',
  imports: [
    DiagnosticsNavComponent,
    RouterOutlet,
    Divider
  ],
  templateUrl: './diagnostics-main.component.html',
  styleUrl: './diagnostics-main.component.scss'
})
export class DiagnosticsMainComponent implements OnDestroy {
  
  siteId = input.required<number, string>({ transform: (value: string) => Number(value) });
  processId = input<number, string>(0, { transform: (value: string) => Number(value) });
  #appContext = inject(AppContextService);

  constructor() {
    toObservable(this.siteId).subscribe(siteId => this.#appContext.siteId.set(siteId));
  }

  ngOnDestroy(): void {
        this.#appContext.siteId.set(0);
    }
  

}
