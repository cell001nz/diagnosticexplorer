import {Component, computed, inject, OnInit} from '@angular/core';
import { MenuItem } from 'primeng/api';
import {AuthService} from "@services/auth.service";
import {Menu} from "primeng/menu";
import {firstValueFrom} from "rxjs";
import {SiteService} from "@services/site.service";

@Component({
  selector: 'app-menu',
    imports: [
        Menu
    ],
  templateUrl: './app-menu.component.html',
  styleUrl: './app-menu.component.scss'
})
export class AppMenuComponent {
    #authService = inject(AuthService);
    #siteService = inject(SiteService);

    menuItems = computed(() => {
        let adminItems: MenuItem[] = [];
        let sites = this.#siteService.sites.value();

        let siteItems: MenuItem[] = sites.map(s => (
            {label: s.name, icon: 'pi pi-fw pi-home', routerLink: ['diagnostics', s.id]}
        ));

        if (this.#siteService.sites.isLoading() && this.#siteService.sites.value().length === 0)
            siteItems = [{label: '...', icon: 'pi pi-fw pi-home'}];
        else if (!siteItems.length)
            siteItems = [{label: 'No Sites', icon: 'pi pi-fw pi-home'}];

        let siteMenu = [ { label: 'Diagnostics', items: siteItems }];

        if (this.#authService.account()?.clientPrincipal?.userRoles.some(r => r.toLowerCase() === 'admin')) {
            adminItems = [
                {
                    label: 'Admin',
                    items: [
                        {label: 'Accounts', icon: 'pi pi-fw pi-home', routerLink: ['admin', 'accounts']},
                    ]
                }
            ];
        }

        return [
            {
                label: 'Home',
                items: [
                    {label: 'Account', icon: 'pi pi-fw pi-home', routerLink: ['account']},
                    {label: 'Sites', icon: 'pi pi-fw pi-home', routerLink: ['sites'], routerLinkActiveOptions: {exact: false}}
                ]
            },
            ...adminItems,
            ...siteMenu,
            {
                label: 'Get Started',
                items: [
                    { label: 'Documentation', icon: 'pi pi-fw pi-book', routerLink: ['documentation'] },

                ]
            }
        ]
    });
}