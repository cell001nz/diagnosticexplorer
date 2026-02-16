import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { RippleModule } from 'primeng/ripple';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'pricing-widget',
    imports: [DividerModule, ButtonModule, RippleModule, RouterModule],
    template: `
        <div class="py-6 px-6 lg:px-20 my-2 md:my-6">
            <div class="text-center mb-6">
                <div class="text-surface-900 dark:text-surface-0 font-normal mb-2 text-4xl">Get Started</div>
                <span class="text-muted-color text-2xl">Three simple steps to start monitoring your applications</span>
            </div>

            <div class="grid grid-cols-12 gap-4 justify-between mt-20 md:mt-0">
                <div class="col-span-12 lg:col-span-4 p-0 md:p-4">
                    <div class="p-4 flex flex-col border-surface-200 dark:border-surface-600 cursor-pointer border-2 hover:border-primary duration-300 transition-all h-full" style="border-radius: 10px">
                        <div class="flex items-center justify-center bg-blue-100 mx-auto my-6" style="width: 4rem; height: 4rem; border-radius: 50%">
                            <span class="text-2xl font-bold text-blue-700">1</span>
                        </div>
                        <div class="text-surface-900 dark:text-surface-0 text-center my-4 text-2xl font-semibold">Install the Package</div>
                        <div class="bg-surface-100 dark:bg-surface-800 p-4 rounded-lg my-4 mx-2">
                            <code class="text-sm">dotnet add package DiagnosticExplorer.Hosting</code>
                        </div>
                        <p-divider class="w-full bg-surface-200"></p-divider>
                        <ul class="my-4 list-none p-0 flex text-surface-900 dark:text-surface-0 flex-col px-4">
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">NuGet package available</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">.NET 5+ supported</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">Zero dependencies</span>
                            </li>
                        </ul>
                    </div>
                </div>

                <div class="col-span-12 lg:col-span-4 p-0 md:p-4 mt-6 md:mt-0">
                    <div class="p-4 flex flex-col border-surface-200 dark:border-surface-600 cursor-pointer border-2 hover:border-primary duration-300 transition-all h-full" style="border-radius: 10px">
                        <div class="flex items-center justify-center bg-green-100 mx-auto my-6" style="width: 4rem; height: 4rem; border-radius: 50%">
                            <span class="text-2xl font-bold text-green-700">2</span>
                        </div>
                        <div class="text-surface-900 dark:text-surface-0 text-center my-4 text-2xl font-semibold">Configure Your App</div>
                        <div class="bg-surface-100 dark:bg-surface-800 p-4 rounded-lg my-4 mx-2">
                            <code class="text-sm whitespace-pre">services.AddDiagnosticExplorer();</code>
                        </div>
                        <p-divider class="w-full bg-surface-200"></p-divider>
                        <ul class="my-4 list-none p-0 flex text-surface-900 dark:text-surface-0 flex-col px-4">
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">One line setup</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">Configuration via appsettings</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">IHostedService integration</span>
                            </li>
                        </ul>
                    </div>
                </div>

                <div class="col-span-12 lg:col-span-4 p-0 md:p-4 mt-6 md:mt-0">
                    <div class="p-4 flex flex-col border-surface-200 dark:border-surface-600 cursor-pointer border-2 hover:border-primary duration-300 transition-all h-full" style="border-radius: 10px">
                        <div class="flex items-center justify-center bg-purple-100 mx-auto my-6" style="width: 4rem; height: 4rem; border-radius: 50%">
                            <span class="text-2xl font-bold text-purple-700">3</span>
                        </div>
                        <div class="text-surface-900 dark:text-surface-0 text-center my-4 text-2xl font-semibold">Start Monitoring</div>
                        <div class="flex flex-col items-center my-4 gap-4">
                            <a pButton pRipple routerLink="/app" label="Open Dashboard" class="p-button-rounded border-0 font-light leading-tight bg-blue-500 text-white"></a>
                        </div>
                        <p-divider class="w-full bg-surface-200"></p-divider>
                        <ul class="my-4 list-none p-0 flex text-surface-900 dark:text-surface-0 flex-col px-4">
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">Real-time monitoring</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">Remote method invocation</span>
                            </li>
                            <li class="py-2">
                                <i class="pi pi-fw pi-check text-xl text-cyan-500 mr-2"></i>
                                <span class="text-lg leading-normal">No application restart</span>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class PricingWidget {}
