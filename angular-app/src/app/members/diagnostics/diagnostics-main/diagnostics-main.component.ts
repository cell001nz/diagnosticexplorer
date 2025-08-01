import {Component, input} from '@angular/core';
import {Splitter} from "primeng/splitter";
import {DiagnosticsNavComponent} from "@app/members/diagnostics/diagnostics-nav/diagnostics-nav.component";
import {RouterOutlet} from "@angular/router";
import {Divider} from "primeng/divider";

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
export class DiagnosticsMainComponent {
  
  siteId = input.required<string>();

}
