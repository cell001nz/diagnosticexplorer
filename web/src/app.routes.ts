import {ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot, Routes} from '@angular/router';
import {AppLayout} from './app/layout/component/app.layout';
import {Notfound} from './app/pages/notfound/notfound';
import {LandingComponent} from "./app/pages/landing/landing.component";
import {DashboardComponent} from "./app/pages/dashboard/dashboard/dashboard.component";
import {AccountComponent} from "./app/pages/account/account.component";
import {TermsComponent} from "./app/terms/terms.component";
import {PrivacyComponent} from "./app/privacy/privacy.component";
import {inject, Injectable, Injector} from "@angular/core";
import {AppAuthService} from "./app/services/app-auth.service";
import {LoginComponent} from "./app/login/login.component";
import {toObservable} from "@angular/core/rxjs-interop";
import {filter, map, take, tap} from "rxjs/operators";


@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
    injector = inject(Injector);

  constructor(private auth: AppAuthService, private router: Router) {}

  canActivate( route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): MaybeAsync<GuardResult> {

      console.log('canActivate', state.url);
      try {
        const encodedRedirect = encodeURIComponent(state.url);     
          if (this.auth.isLoggedIn())
              return true;

          return toObservable(this.auth.account, {injector: this.injector})
              .pipe(
                  filter(acct => !!acct),
                  map(acct => acct.clientPrincipal ? true : this.router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`)),
                  take(1),
                  tap(val => console.log("CanActivate", val))
              );
          
      }
      catch (err) {
          console.log(err);
        const encodedRedirect = encodeURIComponent(state.url);     
        return this.router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`);
      }
  }
}

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
            { path: 'uikit', loadChildren: () => import('./app/pages/uikit/uikit.routes') },
        ]
    },
    { path: 'login', component: LoginComponent },
    { path: 'notfound', component: Notfound },
    { path: 'terms', component: TermsComponent },
    { path: 'privacy', component: PrivacyComponent },
    { path: '**', redirectTo: '/notfound' }
];
