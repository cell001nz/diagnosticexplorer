import {HTTP_INTERCEPTORS, provideHttpClient, withFetch} from '@angular/common/http';
import {ApplicationConfig, inject, Injector, provideAppInitializer} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {provideRouter, withComponentInputBinding, withEnabledBlockingInitialNavigation, withInMemoryScrolling} from '@angular/router';
import Aura from '@primeng/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import {AuthService} from "./app/services/auth.service";
import {MessageService} from "primeng/api";

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withComponentInputBinding(), withInMemoryScrolling({
            anchorScrolling: 'enabled',
            scrollPositionRestoration: 'enabled'
        }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch()),
        provideAnimationsAsync(),
        MessageService,
        providePrimeNG({theme: {preset: Aura, options: {darkModeSelector: '.app-dark'}}}),
         provideAppInitializer(async () => {
             let auth = inject(AuthService);
             await auth.initialiseAsync();
            })           
    ]
};
