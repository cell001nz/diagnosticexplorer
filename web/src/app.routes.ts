import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Notfound } from './app/pages/notfound/notfound';
import {LandingComponent} from "./app/pages/landing/landing.component";
import {DashboardComponent} from "./app/pages/dashboard/dashboard/dashboard.component";
import { MsalGuard } from '@azure/msal-angular';
import {AccountComponent} from "./app/pages/account/account.component";
import {TermsComponent} from "./app/terms/terms.component";
import {PrivacyComponent} from "./app/privacy/privacy.component";

export const appRoutes: Routes = [
    { path: '', component: LandingComponent },
    {
        path: 'app',
        component: AppLayout,
        canActivate: [MsalGuard],
        children: [
            { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
            { path: 'account', component: AccountComponent },
            { path: 'dashboard', component: DashboardComponent },
            { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'terms', component: TermsComponent },
    { path: 'privacy', component: PrivacyComponent },
    { path: '**', redirectTo: '/notfound' }
];
