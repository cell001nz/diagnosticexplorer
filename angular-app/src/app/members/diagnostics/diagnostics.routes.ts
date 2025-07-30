import {DiagnosticsMainComponent} from "./diagnostics-main/diagnostics-main.component";
import { Routes } from '@angular/router';
import {DiagnosticsViewComponent} from "@app/members/diagnostics/diagnostics-view/diagnostics-view.component";

export default [
    { path: ':siteId', component: DiagnosticsMainComponent, children: [
        { path: ':processId', component: DiagnosticsViewComponent }
    ]},
] as Routes;
