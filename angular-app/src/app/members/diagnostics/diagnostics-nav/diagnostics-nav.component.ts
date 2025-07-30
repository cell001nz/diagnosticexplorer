import {Component, computed, inject, input} from '@angular/core';
import {AuthService} from "@services/auth.service";
import {rxResource} from "@angular/core/rxjs-interop";
import {TableModule} from "primeng/table";
import {ErrMsgPipe} from "@pipes/err-msg.pipe";
import {JsonPipe} from "@angular/common";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {SiteService} from "@services/site.service";

@Component({
  selector: 'app-diagnostics-nav',
  imports: [
    TableModule,
    ErrMsgPipe,
    RouterLink
  ],
  templateUrl: './diagnostics-nav.component.html',
  styleUrl: './diagnostics-nav.component.scss'
})
export class DiagnosticsNavComponent {

  siteId = input.required<string>();
  
  auth = inject(AuthService);
  #sitesService = inject(SiteService);
  
  processes = rxResource({
    defaultValue: [],
    params: () => ({siteId: this.siteId()}),
    stream: ({params: p}) => this.#sitesService.getProcesses(p.siteId)
  })
  
}
