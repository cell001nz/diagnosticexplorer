import {Component, inject, OnInit, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {AuthService} from '@services/auth.service';
import {InputTextModule} from 'primeng/inputtext';
import {ButtonModule} from 'primeng/button';
import {MessageModule} from 'primeng/message';

@Component({
    selector: 'app-complete-profile',
    imports: [
        ReactiveFormsModule,
        InputTextModule,
        ButtonModule,
        MessageModule,
    ],
    templateUrl: './complete-profile.component.html',
    styleUrl: './complete-profile.component.scss'
})
export class CompleteProfileComponent implements OnInit {
    #fb = inject(FormBuilder);
    #auth = inject(AuthService);
    #router = inject(Router);

    saving = signal(false);
    errorMessage = signal<string | null>(null);

    form = this.#fb.group({
        name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    });

    ngOnInit() {
        const account = this.#auth.account();
        const authMe = this.#auth.authMe();

        // Pre-populate email from SWA userDetails (contains email for Google, UPN for AAD)
        const userDetails = authMe?.clientPrincipal?.userDetails ?? '';
        if (account?.email) {
            this.form.patchValue({email: account.email});
        } else if (userDetails.includes('@')) {
            this.form.patchValue({email: userDetails});
        }

        // Pre-populate name if it's not just the opaque userId
        if (account?.name && account.name !== authMe?.clientPrincipal?.userId) {
            this.form.patchValue({name: account.name});
        }
    }

    async submit() {
        if (this.form.invalid) return;

        this.saving.set(true);
        this.errorMessage.set(null);

        try {
            const {name, email} = this.form.value;
            const updated = await this.#auth.updateProfile(name!, email!);
            this.#auth.account.set(updated);
            await this.#router.navigate(['/app']);
        } catch (e: any) {
            this.errorMessage.set(e?.error ?? 'Failed to save profile. Please try again.');
        } finally {
            this.saving.set(false);
        }
    }
}


