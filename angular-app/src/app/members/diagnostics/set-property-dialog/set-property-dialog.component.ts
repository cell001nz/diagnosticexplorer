import {ChangeDetectionStrategy, Component, inject, Inject, input, OnInit, signal} from '@angular/core';
import {DynamicDialogRef} from "primeng/dynamicdialog";
import {PromptResult} from "@util/PromptData";
import {Card} from "primeng/card";
import {FormsModule} from "@angular/forms";
import {Fieldset} from "primeng/fieldset";
import {toObservable} from "@angular/core/rxjs-interop";
import {FloatLabel} from "primeng/floatlabel";
import {InputText} from "primeng/inputtext";
import {ButtonDirective} from "primeng/button";

@Component({
    selector: 'app-set-property-dialog',
    templateUrl: './set-property-dialog.component.html',
    styleUrls: ['./set-property-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        Card,
        FormsModule,
        Fieldset,
        FloatLabel,
        InputText,
        ButtonDirective
    ]
})
export class SetPropertyDialogComponent implements OnInit {

    text = input.required<string>();
    value = input.required<string>();
    editValue = signal('');

    ref = inject(DynamicDialogRef);
    constructor() {
        toObservable(this.value).subscribe(v => this.editValue.set(v));
    }

    ngOnInit(): void {
    }


    onCancelClick(): void {
        this.ref.close(new PromptResult('Cancel', ''));
    }

    onOkClick(): void {
        this.ref.close(new PromptResult('OK', this.editValue()));
    }

    handleKeyUp(evt: KeyboardEvent) {
        if (evt.key === 'Enter')
            this.onOkClick();

        if (evt.key === 'Escape')
            this.onCancelClick();
    }
}
