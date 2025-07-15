import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { StyleClassModule } from 'primeng/styleclass';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { HomeWidget } from './components/home-widget.component';
import { FeaturesWidget } from './components/featureswidget';
import { HighlightsWidget } from './components/highlightswidget';
import { PricingWidget } from './components/pricingwidget';
import { FooterWidget } from './components/footerwidget';
import {LandingLayout} from "./components/landinglayout.component";

@Component({
    selector: 'app-landing',
    standalone: true,
    imports: [RouterModule, HomeWidget, FeaturesWidget, HighlightsWidget, PricingWidget, FooterWidget, RippleModule, StyleClassModule, ButtonModule, DividerModule, LandingLayout],
    template: `
        <landing-layout>
            <home-widget id="home"  />            
            <features-widget id="features" class="scroll-mt-100" />
            <highlights-widget id="highlights" class="scroll-mt-200" />
            <pricing-widget id="pricing"  class="scroll-mt-100"  />
        </landing-layout>
    `
})
export class LandingComponent {}
