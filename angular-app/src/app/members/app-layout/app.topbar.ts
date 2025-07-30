import {Component, inject} from '@angular/core';
import { MenuItem } from 'primeng/api';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StyleClassModule } from 'primeng/styleclass';
import { AppConfigurator } from '@app/app-configurator/app.configurator';
import { LayoutService } from '@services/layout.service';
import {Menu} from "primeng/menu";
import {AuthService} from "@services/auth.service";

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, StyleClassModule, AppConfigurator, Menu],
    template: ` 
     <div class="flex w-full items-center layout-topbar text-xl gap-4 ">
            <button class="cursor-pointer layout-menu-button" (click)="layoutService.onMenuToggle()">
                <i class="pi pi-bars"></i>
            </button>
     
            <a class="flex-1 flex items-center gap-1" routerLink="/">
                <i class="bi bi-bug text-xl hidden md:block"></i>
                <span>Diagnostic Explorer</span>
            </a>

            <button type="button" class="hidden md:block cursor-pointer" (click)="toggleDarkMode()">
                <i class="pi" [class.pi-moon]="layoutService.isDarkTheme()" [class.pi-sun]="!layoutService.isDarkTheme()"></i>
            </button>
 
            <button class="hidden md:block cursor-pointer" pStyleClass="@next"
                enterFromClass="hidden" enterActiveClass="animate-scalein"
                leaveToClass="hidden" leaveActiveClass="animate-fadeout"
                [hideOnOutsideClick]="true">
                <i class="pi pi-palette"></i>
            </button>
            <app-configurator />
         
            
            <button type="button" class="layout-topbar-action">
                <i class="pi pi-user" (click)="menu.toggle($event)"></i>
                <span>Profile</span>
            </button>
             <p-menu #menu [model]="items" [popup]="true" />
                    
            <button class="md:hidden" pStyleClass="@next" enterFromClass="hidden" enterActiveClass="animate-scalein" leaveToClass="hidden" leaveActiveClass="animate-fadeout" [hideOnOutsideClick]="true">
                <i class="pi pi-ellipsis-v"></i>
            </button>

            <div class="layout-topbar-menu flex md:hidden flex-col items-start gap-y-2">
                    <button type="button" class="flex gap-2 items-center">
                        <i class="pi pi-user"></i>
                        <span>Profile</span>
                    </button>
                    <button type="button" class="flex gap-2 items-center" (click)="toggleDarkMode()">
                        <i class="pi" [class.pi-moon]="layoutService.isDarkTheme()" [class.pi-sun]="!layoutService.isDarkTheme()"></i>
                        <span>Light/Dark</span>
                    </button>
                   <button class="flex gap-2 items-center cursor-pointer" pStyleClass="@next"
                            enterFromClass="hidden" enterActiveClass="animate-scalein"
                            leaveToClass="hidden" leaveActiveClass="animate-fadeout"
                            [hideOnOutsideClick]="true">
                            <i class="pi pi-palette"></i>
                            <span>Palette</span>
                        </button>
                        <app-configurator />
                
                
                </div>
    </div>
    `
})
export class AppTopbar {
    
    #authSvc = inject(AuthService);
    
    items: MenuItem[] = [
        {label: 'Logout', icon: 'pi pi-sign-out', styleClass: 'text-base', command: () => this.#authSvc.logout() },
    ]
    
    

    constructor(public layoutService: LayoutService) {}

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }

}
