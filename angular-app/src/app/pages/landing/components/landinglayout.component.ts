import { Component } from '@angular/core';
import { StyleClassModule } from 'primeng/styleclass';
import { Router, RouterModule } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import {NgOptimizedImage} from "@angular/common";
import {FeaturesWidget} from "./featureswidget";
import {FooterWidget} from "./footerwidget";
import {HomeWidget} from "./home-widget.component";
import {HighlightsWidget} from "./highlightswidget";
import {PricingWidget} from "./pricingwidget";
import {TopbarWidget} from "./topbarwidget.component";
import {AppFloatingConfigurator} from "../../../layout/component/app.floatingconfigurator";

@Component({
    selector: 'landing-layout',
    imports: [RouterModule, StyleClassModule, ButtonModule, RippleModule, TopbarWidget, AppFloatingConfigurator, FooterWidget],
    templateUrl: "./landinglayout.component.html",
    styleUrl: "./landinglayout.component.scss"
})
export class LandingLayout {
    
}
