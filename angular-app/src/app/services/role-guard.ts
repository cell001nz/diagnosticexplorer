import {inject, Injectable, Injector} from "@angular/core";
import {ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot} from "@angular/router";
import {AuthService} from "./auth.service";

@Injectable({providedIn: 'root'})
export class RoleGuard implements CanActivate {
    #auth = inject(AuthService);
     #router = inject(Router);
    canActivate(route: ActivatedRouteSnapshot,
                state: RouterStateSnapshot): MaybeAsync<GuardResult> {

        try {
            return this.#auth.getAccount()
                .then(acct => !!acct.clientPrincipal && acct.clientPrincipal.userRoles.includes('admin')
                ? true : this.#router.parseUrl(`/app/not-authorized`));

        } catch (err) {
            console.log(err);
            return false;
        }
    }
}