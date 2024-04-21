import { Component, ViewChild, TemplateRef } from '@angular/core'
import { MatDialog, MatDialogRef } from '@angular/material/dialog';

@Component({
	selector: 'message-box',
	templateUrl: './messageBox.component.html',
	styleUrls: ['../../../app.component.css'],
})
export class MessageBoxComponent {

	@ViewChild('messageBoxDialog') private messageBoxDialog!: TemplateRef<any>;
	@ViewChild('webApiErrorDialog') private webApiErrorDialog!: TemplateRef<any>;
	@ViewChild('confirmationDialog') private confirmationDialog!: TemplateRef<any>;
	public dialogRef!: MatDialogRef<any, any>;

	// Generic message
	public headerText!: string;
	public bodyText!: string;

	// Web API error
	public webApiErrorUrl!: string;
	public webApiErrorDetails!: any[];

	// Confirmation dialog
	public confirmationModalMessageText!: string;
	public confirmationModalButtonText!: string;
	private confirmationModalAction: any;

	constructor(
		private dialog: MatDialog,
	) {
	}

	// *** Generic message ***

	public Show(headerText: string, bodyText: string) {
		this.headerText = headerText;
		this.bodyText = bodyText;
		this.dialogRef = this.dialog.open(this.messageBoxDialog);
	}

	// *** Web API error ***

	public ShowWebApiError(error: any) {
		if (error.url) {
			this.webApiErrorUrl = error.url.split('?')[0];
			let details = this.ExtractErrorDetails(error);
			this.webApiErrorDetails = details;
		}
		else {
			this.webApiErrorUrl = "Unknown URL";
			this.webApiErrorDetails = [
				{
					type: "HTTP Error",
					message: "Ensure that the Web API is running and try again"
				}
			];
		}

		this.dialogRef = this.dialog.open(this.webApiErrorDialog);
	}

	private ExtractErrorDetails(error: any): any[] {
		const name = error.name ?? 'An unhandled exception occurred';
		const message = error.error && typeof error.error === 'string' ? error.error : (error.message ?? 'Unknown error');
		const details = [
			{
				type: name,
				message: message
			}
		];
		return details;
	}

	// *** Confirmation dialog ***

	public ShowConfirmationModal(messageText: string, buttonText: string, action: any) {
		this.confirmationModalMessageText = messageText;
		this.confirmationModalButtonText = buttonText;
		this.confirmationModalAction = function () {
			action();
			this.dialogRef.close();
		}
		this.dialogRef = this.dialog.open(this.confirmationDialog);
	}

	public ConfirmAction() {
		this.confirmationModalAction();
	}

}
