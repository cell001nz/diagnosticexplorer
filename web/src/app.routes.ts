import {Routes} from '@angular/router';
import {AppLayout} from './app/layout/component/app.layout';
import {Notfound} from './app/pages/notfound/notfound';
import {LandingComponent} from "./app/pages/landing/landing.component";
import {DashboardComponent} from "./app/pages/dashboard/dashboard/dashboard.component";
import {AccountComponent} from "./app/pages/account/account.component";
import {TermsComponent} from "./app/terms/terms.component";
import {PrivacyComponent} from "./app/privacy/privacy.component";
import {LoginComponent} from "./app/login/login.component";
import {AuthGuard} from "./app/services/auth-guard.service";
import {AdminAccountsComponent} from "./app/admin/admin-accounts/admin-accounts.component";
import {DocumentationComponent} from "./app/pages/documentation/documentation.component";


export const appRoutes: Routes = [
    { path: '', component: LandingComponent },
    {
        path: 'app',
        component: AppLayout,
        canActivate: [AuthGuard],
        children: [
            { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
            { path: 'account', component: AccountComponent },
            { path: 'dashboard', component: DashboardComponent },
            { path: 'documentation', component: DocumentationComponent },
            // { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
            {
                path: 'admin',
                canActivate: [AuthGuard],
                children: [
                    { path: '', pathMatch: 'full', redirectTo: 'accounts' },
                    { path: 'accounts', component: AdminAccountsComponent },
                ]
            },
        ]
    },
    { path: 'login', component: LoginComponent },
    { path: 'notfound', component: Notfound },
    { path: 'terms', component: TermsComponent },
    { path: 'privacy', component: PrivacyComponent },
    { path: '**', redirectTo: '/notfound' }
];
