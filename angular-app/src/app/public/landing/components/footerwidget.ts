import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
    selector: 'footer-widget',
    imports: [RouterModule],
    template: `
        <div class="py-12 px-12 mx-0 mt-20 lg:mx-20">
            <div class="grid grid-cols-12 gap-4">
                <div class="col-span-12 md:col-span-2">
                    <a (click)="router.navigate(['/pages/landing'], { fragment: 'home' })" class="flex flex-wrap items-center justify-center md:justify-start md:mb-0 mb-6 cursor-pointer gap-2">
                        <i class="bi bi-bug text-2xl"></i>
                        <h5 class="font-medium text-3xl text-surface-900 dark:text-surface-0">Diagnostic Explorer</h5>
                    </a>
                </div>

                <div class="col-span-12 md:col-span-10">
                    <div class="grid grid-cols-12 gap-8 text-center md:text-left">
                        <div class="col-span-12 md:col-span-3">
                            <h4 class="font-medium text-2xl leading-normal mb-6 text-surface-900 dark:text-surface-0">Documentation</h4>
                            <a href="https://www.codeproject.com/Articles/28aboratori/Diagnostic-Explorer" target="_blank" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">CodeProject Article</a>
                            <a routerLink="/app" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">Dashboard</a>
                        </div>

                        <div class="col-span-12 md:col-span-3">
                            <h4 class="font-medium text-2xl leading-normal mb-6 text-surface-900 dark:text-surface-0">Resources</h4>
                            <a routerLink="/" fragment="features" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">Features</a>
                            <a routerLink="/" fragment="highlights" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">How It Works</a>
                            <a routerLink="/" fragment="pricing" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">Get Started</a>
                        </div>

                        <div class="col-span-12 md:col-span-3">
                            <h4 class="font-medium text-2xl leading-normal mb-6 text-surface-900 dark:text-surface-0">Open Source</h4>
                            <a href="https://github.com" target="_blank" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">GitHub</a>
                            <a class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">LGPL License</a>
                        </div>

                        <div class="col-span-12 md:col-span-3">
                            <h4 class="font-medium text-2xl leading-normal mb-6 text-surface-900 dark:text-surface-0">Legal</h4>
                            <a routerLink="/privacy" class="leading-normal text-xl block cursor-pointer mb-2 text-surface-700 dark:text-surface-100">Privacy Policy</a>
                            <a routerLink="/terms" class="leading-normal text-xl block cursor-pointer text-surface-700 dark:text-surface-100">Terms of Service</a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class FooterWidget {
    constructor(public router: Router) {}
}
