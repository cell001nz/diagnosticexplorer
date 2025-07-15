import {HTTP_INTERCEPTORS, provideHttpClient, withFetch} from '@angular/common/http';
import {ApplicationConfig, inject, Injector, provideAppInitializer} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeng/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import {
    MSAL_GUARD_CONFIG,
    MSAL_INSTANCE,
    MsalBroadcastService,
    MsalGuard,
    MsalGuardConfiguration,
    MsalInterceptor,
    MsalService
} from "@azure/msal-angular";


import { msalConfig, loginRequest } from './auth-config';
import {InteractionType, IPublicClientApplication, PublicClientApplication} from "@azure/msal-browser";
import {AppAuthService} from "./app/services/app-auth.service";

/**
 * Here we pass the configuration parameters to create an MSAL instance.
 * For more info, visit: https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-angular/docs/v2-docs/configuration.md
 */
export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication(msalConfig);
}

/**
 * Set your default interaction type for MSALGuard here. If you have any
 * additional scopes you want the user to consent upon login, add them here as well.
 */
export function MsalGuardConfigurationFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: loginRequest
  };
}

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({
            anchorScrolling: 'enabled',
            scrollPositionRestoration: 'enabled'
        }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch()),
        provideAnimationsAsync(),
        providePrimeNG({theme: {preset: Aura, options: {darkModeSelector: '.app-dark'}}}),
        {
            provide: HTTP_INTERCEPTORS,
            useClass: MsalInterceptor,
            multi: true,
        },
        {
            provide: MSAL_INSTANCE,
            useFactory: MSALInstanceFactory,
        },
        {
            provide: MSAL_GUARD_CONFIG,
            useFactory: MsalGuardConfigurationFactory,
        },
        MsalService,
        MsalBroadcastService,
        MsalGuard,
         provideAppInitializer(async () => {
             // let authSvc = inject(AppAuthService);
             // await authSvc.initialiseAsync()
            // const initializerFn = ((injector: Injector) => async () => {
            //     await injector.get(AppAuthService).initialiseAsync()
            })
            
    ]
};
