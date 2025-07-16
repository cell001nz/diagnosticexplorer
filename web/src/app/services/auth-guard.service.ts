import {inject, Injectable, Injector} from "@angular/core";
import {ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot} from "@angular/router";
import {toObservable} from "@angular/core/rxjs-interop";
import {filter, map, take, tap} from "rxjs/operators";
import {AppAuthService} from "./app-auth.service";

@Injectable({providedIn: 'root'})
export class AuthGuard implements CanActivate {
    #injector = inject(Injector);
    #auth = inject(AppAuthService);
    #router = inject(Router);
    
    canActivate(route: ActivatedRouteSnapshot,
                state: RouterStateSnapshot): MaybeAsync<GuardResult> {

        console.log('canActivate', state.url);
        try {
            const encodedRedirect = encodeURIComponent(state.url);
            if (this.#auth.isLoggedIn())
                return true;

            return toObservable(this.#auth.account, {injector: this.#injector})
                .pipe(
                    filter(acct => !!acct),
                    map(acct => acct.clientPrincipal ? true : this.#router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`)),
                    take(1),
                    tap(val => console.log("CanActivate", val))
                );

        } catch (err) {
            console.log(err);
            const encodedRedirect = encodeURIComponent(state.url);
            return this.#router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`);
        }
    }
}