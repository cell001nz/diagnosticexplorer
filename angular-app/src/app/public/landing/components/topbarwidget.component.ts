import {Component, computed, inject} from '@angular/core';
import { StyleClassModule } from 'primeng/styleclass';
import { Router, RouterModule } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import {AuthService} from "@services/auth.service";

@Component({
    selector: 'topbar-widget',
    imports: [RouterModule, StyleClassModule, ButtonModule, RippleModule],
    template: `
        
        <a class="flex items-center" href="#">
            <i class="bi bi-bug text-2xl"></i>
            <span class="whitespace-nowrap text-surface-900 dark:text-surface-0 font-medium text-2xl leading-normal ml-2 mb-1 mr-20">Diagnostic Explorer</span>
        </a>
        
        <a pButton [text]="true" severity="secondary" [rounded]="true" pRipple class="lg:hidden!" pStyleClass="@next" enterClass="hidden" leaveToClass="hidden" [hideOnOutsideClick]="true">
            <i class="pi pi-bars text-2xl!"></i>
        </a>

        <div class="items-center  grow justify-between hidden lg:flex absolute lg:static w-full left-0 top-full px-12 lg:px-0 z-20 rounded-border">
            <div class=" p-0 m-0 flex lg:items-center select-none flex-col lg:flex-row cursor-pointer gap-8">
                    <a routerLink="/" fragment="home" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Home</span>
                    </a>
                  
                   <a routerLink="/" fragment="features" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Features</span>
                    </a>

                    <a routerLink="/" fragment="highlights" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>How It Works</span>
                    </a>
                
                    <a routerLink="/" fragment="pricing" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Get Started</span>
                    </a>
                
                    <a routerLink="/app" pRipple class="px-0 py-4 text-surface-900 dark:text-surface-0 font-medium text-xl">
                        <span>Dashboard</span>
                    </a>
                
                </div>
            <div class="flex border-t lg:border-t-0 border-surface py-4 lg:py-0 mt-4 lg:mt-0 gap-2">
                @if (isLoggedIn()) {
                    <button pButton pRipple (click)="logoutClick()" [rounded]="true" [text]="true"><span pButtonLabel>Logout</span></button>
                } @else {
                    <button pButton pRipple (click)="loginClick()" [rounded]="true" [text]="true"><span pButtonLabel>Login</span></button>
                }
            </div>
        </div> `
})
export class TopbarWidget {

    #appAuth = inject(AuthService)

    constructor() {
    }

    loginClick() {
        this.#appAuth.login();
    }
    
    logoutClick() {
        this.#appAuth.logout();
    }
    
     isLoggedIn = computed(() => this.#appAuth.isLoggedIn());
}
