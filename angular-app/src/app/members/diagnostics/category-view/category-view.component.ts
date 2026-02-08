import {ChangeDetectionStrategy, Component, inject, input, Input} from '@angular/core';
import {CategoryModel} from "@model/CategoryModel";
import {JsonPipe} from "@angular/common";
import {Accordion, AccordionContent, AccordionHeader, AccordionPanel} from "primeng/accordion";
import {Panel} from "primeng/panel";
import {Fieldset} from "primeng/fieldset";
import {PropModel} from "@model/PropModel";
import {PromptData, PromptResult} from "@util/PromptData";
import {DialogService, DynamicDialogRef} from "primeng/dynamicdialog";
import {SetPropertyDialogComponent} from "@app/members/diagnostics/set-property-dialog/set-property-dialog.component";
import {AppContextService} from "@services/app-context.service";
import {DiagHubService} from "@services/diag-hub.service";
import {MessageService} from "primeng/api";
import {getErrorMsg} from "@util/errorUtil";
import {SetPropertyRequest} from "@domain/SetPropertyRequest";
import {DiagProcess} from "@domain/DiagProcess";

@Component({
  selector: 'app-category-view',
    imports: [
        Panel,
        Fieldset
    ],
  templateUrl: './category-view.component.html',
  styleUrl: './category-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DialogService]
})
export class CategoryViewComponent {
    
    category = input.required<CategoryModel>();
    process = input.required<DiagProcess>();
    #appContext = inject(AppContextService);
    #hubService = inject(DiagHubService);
    dialogService = inject(DialogService);
    ref: DynamicDialogRef | undefined;
    messageService = inject(MessageService);  

  showSetPropertyDialog(prop: PropModel): void {
        const data = new PromptData(prop.getPropertyPath(), prop.value());

        this.ref = this.dialogService.open(SetPropertyDialogComponent, {
            maximizable: false,
            // styleClass: "h-full",
            width: '500px',
            // height: '250px',
            inputValues: {
                text: prop.getPropertyPath(),
                value: prop.value()
            }
        });

        this.ref.onClose.subscribe(async (result: PromptResult) => {
            if (result.button === 'OK')
                await this.setPropertyValue(prop, result.value);
        });
    }

   async setPropertyValue(prop: PropModel, value: string) {
        try {
            const request = new SetPropertyRequest();
            request.processId = this.process()!.id;
            request.siteId = this.process()!.siteId;
            request.path = prop.getPropertyPath();
            request.value = value;

            await this.#hubService.setPropertyValue(request);
            
            // if (result.errorMessage) {
            //     console.log(result);
            //       this.messageService.add({severity: 'error', summary: 'Error', detail: 'Error setting property: ' + result.errorMessage, life: 3_000});
            // } else {
                this.messageService.add({severity: 'information', summary: 'Property Set', detail: 'All good', life: 3_000});
            // }
        } catch (err: any) {
            console.log(err);
            this.messageService.add({severity: 'error', summary: 'Error', detail: 'Error setting property: ' + getErrorMsg(err), life: 3_000});
        }
    }
}
