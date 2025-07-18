import {Routes} from '@angular/router';
import {AppLayout} from './app/layout/component/app.layout';
import {Notfound} from './app/pages/notfound/notfound';
import {LandingComponent} from "./app/pages/landing/landing.component";
import {TermsComponent} from "./app/terms/terms.component";
import {PrivacyComponent} from "./app/privacy/privacy.component";
import {LoginComponent} from "./app/login/login.component";
import {AdminAccountsComponent} from "./app/admin/admin-accounts/admin-accounts.component";
import {DocumentationComponent} from "./app/pages/documentation/documentation.component";
import {RoleGuard} from "./app/services/role-guard";
import {LoginGuard} from "./app/services/login-guard";
import {NotAuthorizedComponent} from "./app/pages/not-authorized/not-authorized.component";
import {SitesComponent} from "./app/sites/sites.component";
import {AccountComponent} from "./app/account/account.component";
import {EditSiteComponent} from "./app/edit-site/edit-site.component";


export const appRoutes: Routes = [
    { path: '', component: LandingComponent },
    {
        path: 'app',
        component: AppLayout,
        canActivate: [LoginGuard],
        children: [
            { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
            { path: 'account', component: AccountComponent },
                    { path: 'sites', component: SitesComponent },
                    { path: 'sites/new', component: EditSiteComponent, data: { editMode: false }  },
                  { path: 'sites/:id', component: EditSiteComponent, data: { editMode: true } },
/*  
            { path: 'sites', children: [
                    { path: '', component: SitesComponent },
                    { path: ':id', component: EditSiteComponent, data: { editMode: true } },
                    { path: 'new', component: EditSiteComponent, data: { editMode: false }  },
                ] },
*/
            { path: 'documentation', component: DocumentationComponent },
            { path: 'not-authorized', component: NotAuthorizedComponent },
            {
                path: 'admin',
                canActivate: [RoleGuard],
                data: { roles: ['admin'] },
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
    { path: 'not-authorized', component: NotAuthorizedComponent },
    { path: '**', redirectTo: '/notfound' }
];
