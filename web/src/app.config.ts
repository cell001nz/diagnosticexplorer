import {HTTP_INTERCEPTORS, provideHttpClient, withFetch} from '@angular/common/http';
import {ApplicationConfig, inject, Injector, provideAppInitializer} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeng/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import {AppAuthService} from "./app/services/app-auth.service";

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({
            anchorScrolling: 'enabled',
            scrollPositionRestoration: 'enabled'
        }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch()),
        provideAnimationsAsync(),
        providePrimeNG({theme: {preset: Aura, options: {darkModeSelector: '.app-dark'}}}),
         provideAppInitializer(async () => {
             let auth = inject(AppAuthService);
             await auth.initialiseAsync();
            })           
    ]
};
