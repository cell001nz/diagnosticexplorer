import {Routes} from '@angular/router';
import {LandingComponent} from "@public/landing/landing.component";
import {TermsComponent} from "@public/terms/terms.component";
import {PrivacyComponent} from "@public/privacy/privacy.component";
import {LoginComponent} from "@public/login/login.component";
import {NotAuthorizedComponent} from "@app/pages/not-authorized/not-authorized.component";
import {NotFoundComponent} from "@app/pages/not-found/not-found.component";

export const appRoutes: Routes = [
    { path: '', component: LandingComponent },
    { path: 'app', loadChildren: () => import('./app/members/members.routes')},
    { path: 'login', component: LoginComponent },
    { path: 'notfound', component: NotFoundComponent },
    { path: 'terms', component: TermsComponent },
    { path: 'privacy', component: PrivacyComponent },
    { path: 'not-authorized', component: NotAuthorizedComponent },
    { path: '**', redirectTo: '/notfound' }
];
