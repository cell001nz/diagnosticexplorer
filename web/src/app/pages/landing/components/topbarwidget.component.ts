import { Component } from '@angular/core';
import { StyleClassModule } from 'primeng/styleclass';
import { Router, RouterModule } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import {NgOptimizedImage} from "@angular/common";

@Component({
    selector: 'topbar-widget',
    imports: [RouterModule, StyleClassModule, ButtonModule, RippleModule],
    template: `
        
        <a class="flex items-center" href="#">
            <img src="/favicon.ico" [width]="16" [height]="16" alt="logo" class="w-6 h-6">
            <span class="whitespace-nowrap text-surface-900 dark:text-surface-0 font-medium text-2xl leading-normal ml-2 mb-1 mr-20">Diagnostic Explorer</span>
        </a>
        
        <a pButton [text]="true" severity="secondary" [rounded]="true" pRipple class="lg:hidden!" pStyleClass="@next" enterClass="hidden" leaveToClass="hidden" [hideOnOutsideClick]="true">
            <i class="pi pi-bars text-2xl!"></i>
        </a>

        <div class="items-center bg-surface-0 dark:bg-surface-900 grow justify-between hidden lg:flex absolute lg:static w-full left-0 top-full px-12 lg:px-0 z-20 rounded-border">
            <ul class="list-none p-0 m-0 flex lg:items-center select-none flex-col lg:flex-row cursor-pointer gap-8">
                <li>
                    <a routerLink="/" fragment="home" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Home</span>
                    </a>
                </li>
                <li>
                    <a routerLink="/" fragment="features" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Features</span>
                    </a>
                </li>
                <li>
                    <a routerLink="/" fragment="highlights" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Highlights</span>
                    </a>
                </li>
                <li>
                    <a routerLink="/" fragment="pricing" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Pricing</span>
                    </a>
                </li>
            </ul>
            <div class="flex border-t lg:border-t-0 border-surface py-4 lg:py-0 mt-4 lg:mt-0 gap-2">
                <button pButton pRipple routerLink="/auth/login" [rounded]="true" [text]="true"><span pButtonLabel>Login</span></button>
                <button pButton pRipple routerLink="/auth/register" [rounded]="true"><span pButtonLabel>Register</span></button>
            </div>
        </div> `
})
export class TopbarWidget {
    constructor(public router: Router) {}
}
