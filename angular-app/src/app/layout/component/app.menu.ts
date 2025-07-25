import {Component, inject, OnInit} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import {AuthService} from "../../services/auth.service";

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        @for (item of model; track item.label; let i = $index) {
            @if (item.separator) { 
                <li class="menu-separator"></li> 
            } @else {
                <li app-menuitem [item]="item" [index]="i" [root]="true"></li>
            } 
        }
    </ul> `
})
export class AppMenu implements OnInit {
    model: MenuItem[] = [];
    #authService = inject(AuthService);
    
    
    
    ngOnInit() {
        
        let adminItems: MenuItem[] = [];
        
        if (this.#authService.account()?.clientPrincipal?.userRoles.some(r => r.toLowerCase() === 'admin'))
            adminItems = [
                {
                    label: 'Admin',
                    items: [
                        { label: 'Accounts', icon: 'pi pi-fw pi-home', routerLink: ['admin', 'accounts'] },
                    ]
                }
            ];
        
        this.model = [
            {
                label: 'Home',
                items: [
                    { label: 'Account', icon: 'pi pi-fw pi-home', routerLink: ['account'] },
                    { label: 'Sites', icon: 'pi pi-fw pi-home', routerLink: ['sites'], routerLinkActiveOptions: { exact: false} }
                ]
            },
            ...adminItems,
            {
                label: 'Get Started',
                items: [
                    {
                        label: 'Documentation',
                        icon: 'pi pi-fw pi-book',
                        routerLink: ['documentation']
                    },
                  
                ]
            }

/*            {
                label: 'Hierarchy',
                items: [
                    {
                        label: 'Submenu 1',
                        icon: 'pi pi-fw pi-bookmark',
                        items: [
                            {
                                label: 'Submenu 1.1',
                                icon: 'pi pi-fw pi-bookmark',
                                items: [
                                    { label: 'Submenu 1.1.1', icon: 'pi pi-fw pi-bookmark' },
                                    { label: 'Submenu 1.1.2', icon: 'pi pi-fw pi-bookmark' },
                                    { label: 'Submenu 1.1.3', icon: 'pi pi-fw pi-bookmark' }
                                ]
                            },
                            {
                                label: 'Submenu 1.2',
                                icon: 'pi pi-fw pi-bookmark',
                                items: [{ label: 'Submenu 1.2.1', icon: 'pi pi-fw pi-bookmark' }]
                            }
                        ]
                    },
                    {
                        label: 'Submenu 2',
                        icon: 'pi pi-fw pi-bookmark',
                        items: [
                            {
                                label: 'Submenu 2.1',
                                icon: 'pi pi-fw pi-bookmark',
                                items: [
                                    { label: 'Submenu 2.1.1', icon: 'pi pi-fw pi-bookmark' },
                                    { label: 'Submenu 2.1.2', icon: 'pi pi-fw pi-bookmark' }
                                ]
                            },
                            {
                                label: 'Submenu 2.2',
                                icon: 'pi pi-fw pi-bookmark',
                                items: [{ label: 'Submenu 2.2.1', icon: 'pi pi-fw pi-bookmark' }]
                            }
                        ]
                    }
                ]
            },*/
        ];
    }
}
