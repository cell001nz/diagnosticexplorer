import {Component, inject, resource} from '@angular/core';
import {JsonPipe} from "@angular/common";
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { RouterModule } from '@angular/router';
import {AuthService} from "@services/auth.service";

@Component({
    selector: 'home-widget',
    imports: [ButtonModule, RippleModule, RouterModule, JsonPipe],
    template: `
        <div
            class="flex flex-col pt-6 px-6 lg:px-20 overflow-hidden anchor-target"
            style="background: linear-gradient(0deg, rgba(255, 255, 255, 0.2), rgba(255, 255, 255, 0.2)), radial-gradient(77.36% 256.97% at 77.36% 57.52%, rgb(200, 230, 255) 0%, rgb(180, 220, 250) 100%); clip-path: ellipse(150% 87% at 93% 13%)"
        >
            <div class="mx-6 md:mx-20 mt-0 md:mt-6">
                <h1 class="text-6xl font-bold text-gray-900! leading-tight">
                    <span class="font-light block">Real-time .NET</span>
                    Diagnostic Toolset
                </h1>
                <p class="text-xl text-gray-700 mt-4 max-w-2xl">
                    Monitor, debug, and explore your .NET applications in real-time.
                    Expose properties, track rates, trace execution flow, and invoke methods - all without stopping your application.
                </p>
                <pre class="text-xl text-gray-700 mt-4 max-w-2xl">
                    Status: {{status.status()}} {{(status.isLoading() ? "Loading" : status.error() ? (status.error() | json) : status.value())}}
                </pre>
                <div class="flex gap-4 mt-8">
                    <a pButton pRipple [rounded]="true" routerLink="/app" class="text-xl! px-6!">
                        Get Started!
                    </a>
                    <a pButton pRipple [rounded]="true" [outlined]="true" href="https://www.codeproject.com/Articles/28aboratori/Diagnostic-Explorer" target="_blank" class="text-xl! px-6!">
                        Documentation
                    </a>
                </div>
            </div>
                <div class="flex justify-center md:justify-end mt-8 pb-8">
                <div class="bg-surface-0 dark:bg-surface-900 p-6 rounded-lg shadow-lg max-w-lg">
                    <pre class="text-sm text-surface-700 dark:text-surface-200 overflow-x-auto"><code class="language-csharp">// Register your object with diagnostics
DiagnosticManager.Register(this, "MyWidget", "Widgets");

// Expose properties with attributes
[Property(AllowSet = true)]
public string Status &#123; get; set; &#125;

// Track rates automatically
[RateProperty]
public RateCounter RequestRate &#123; get; &#125;

// Trace execution flow
using var scope = new TraceScope();
scope.Trace("Processing started...");</code></pre>
                </div>
            </div>
        </div>
    `
})
export class HomeWidget {

    #authService = inject(AuthService);

    status = resource({
        loader: () => this.#authService.getStatus()
    });

}
