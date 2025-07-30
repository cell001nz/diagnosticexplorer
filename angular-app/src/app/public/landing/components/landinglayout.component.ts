import { Component } from '@angular/core';
import { StyleClassModule } from 'primeng/styleclass';
import { RouterModule } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import {FooterWidget} from "./footerwidget";
import {TopbarWidget} from "./topbarwidget.component";
import {AppFloatingConfigurator} from "@app/app-configurator/app.floatingconfigurator";
 

@Component({
    selector: 'landing-layout',
    imports: [RouterModule, StyleClassModule, ButtonModule, RippleModule, TopbarWidget, AppFloatingConfigurator, FooterWidget, AppFloatingConfigurator],
    templateUrl: "./landinglayout.component.html",
    styleUrl: "./landinglayout.component.scss"
})
export class LandingLayout {
    
}
