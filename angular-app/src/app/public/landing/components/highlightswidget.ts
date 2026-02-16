import { Component } from '@angular/core';

@Component({
    selector: 'highlights-widget',
    template: `
        <div class="py-6 px-6 lg:px-20 mx-0 my-12 lg:mx-20">
            <div class="text-center">
                <div class="text-surface-900 dark:text-surface-0 font-normal mb-2 text-4xl">How It Works</div>
                <span class="text-muted-color text-2xl">Simple integration, powerful results</span>
            </div>

            <div class="grid grid-cols-12 gap-4 mt-20 pb-2 md:pb-20">
                <div class="flex justify-center col-span-12 lg:col-span-6 bg-purple-100 p-6 order-1 lg:order-0" style="border-radius: 8px">
                    <pre class="text-sm text-surface-700 overflow-x-auto"><code>// 1. Register your objects
public class MyService : IHostedService
&#123;
    public MyService()
    &#123;
        DiagnosticManager.Register(
            this, "MyService", "Services");
    &#125;

    // 2. Expose properties with attributes
    [Property(AllowSet = true)]
    public string Status &#123; get; set; &#125;

    [RateProperty]
    public RateCounter ProcessingRate &#123; get; &#125;

    // 3. Add diagnostic methods
    [DiagnosticMethod]
    public void ResetCounters() &#123; ... &#125;
&#125;</code></pre>
                </div>

                <div class="col-span-12 lg:col-span-6 my-auto flex flex-col lg:items-end text-center lg:text-right gap-4">
                    <div class="flex items-center justify-center bg-purple-200 self-center lg:self-end" style="width: 4.2rem; height: 4.2rem; border-radius: 10px">
                        <i class="pi pi-fw pi-code text-4xl! text-purple-700"></i>
                    </div>
                    <div class="leading-none text-surface-900 dark:text-surface-0 text-3xl font-normal">Simple Integration</div>
                    <span class="text-surface-700 dark:text-surface-100 text-2xl leading-normal ml-0 md:ml-2" style="max-width: 650px">
                        Add a few attributes to your existing classes and register them with the DiagnosticManager. 
                        No major refactoring required - just decorate your properties and methods with simple attributes.
                    </span>
                </div>
            </div>

            <div class="grid grid-cols-12 gap-4 my-20 pt-2 md:pt-20">
                <div class="col-span-12 lg:col-span-6 my-auto flex flex-col text-center lg:text-left lg:items-start gap-4">
                    <div class="flex items-center justify-center bg-yellow-200 self-center lg:self-start" style="width: 4.2rem; height: 4.2rem; border-radius: 10px">
                        <i class="pi pi-fw pi-desktop text-3xl! text-yellow-700"></i>
                    </div>
                    <div class="leading-none text-surface-900 dark:text-surface-0 text-3xl font-normal">Real-time Web Dashboard</div>
                    <span class="text-surface-700 dark:text-surface-100 text-2xl leading-normal mr-0 md:mr-2" style="max-width: 650px">
                        Monitor all your registered applications from a modern web interface. View property values in real-time, 
                        track rate counters, invoke diagnostic methods, and explore trace scopes - all without stopping your application.
                    </span>
                </div>

                <div class="flex justify-end items-center order-1 sm:order-2 col-span-12 lg:col-span-6 bg-yellow-100 p-6" style="border-radius: 8px">
                    <div class="grid grid-cols-2 gap-4 w-full">
                        <div class="bg-white p-4 rounded shadow">
                            <div class="text-sm text-gray-500">Status</div>
                            <div class="text-lg font-semibold text-green-600">Running</div>
                        </div>
                        <div class="bg-white p-4 rounded shadow">
                            <div class="text-sm text-gray-500">Uptime</div>
                            <div class="text-lg font-semibold">4h 23m</div>
                        </div>
                        <div class="bg-white p-4 rounded shadow">
                            <div class="text-sm text-gray-500">Request Rate</div>
                            <div class="text-lg font-semibold text-blue-600">152/sec</div>
                        </div>
                        <div class="bg-white p-4 rounded shadow">
                            <div class="text-sm text-gray-500">Active Connections</div>
                            <div class="text-lg font-semibold">47</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class HighlightsWidget {}
