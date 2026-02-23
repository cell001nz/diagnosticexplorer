import {Component, OnInit} from '@angular/core';
import {LandingLayout} from "@public/landing/components/landinglayout.component";

@Component({
    selector: 'app-logout',
    imports: [LandingLayout],
    templateUrl: './logout.component.html',
    styleUrl: './logout.component.scss'
})
export class LogoutComponent implements OnInit {

    ngOnInit() {
        setTimeout(() => {
            window.location.assign('/.auth/logout');
        }, 1500);
    }
}

