import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiDatabases.component.html',
	styleUrls: [
		'../../app.component.css',
	]
})
export class CoreApiDatabasesComponent implements OnInit {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	@ViewChild('createDatabaseDialog') private createDatabaseDialog!: TemplateRef<any>;
	@ViewChild('deleteDatabaseDialog') private deleteDatabaseDialog!: TemplateRef<any>;
	public busyIndex: number = 0;
	public dialogRef!: MatDialogRef<any, any>;
	public form!: FormGroup;
	public gridColumns!: any[] | null;
	public gridData!: any[] | null;

	constructor(
		private dialog: MatDialog,
		private formBuilder: FormBuilder,
		private webapi: WebApiService,
	) {
	}

	public ngOnInit() {
	}	

	public ViewDatabases() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/databases', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.gridColumns = ['DatabaseId', 'ResourceId', 'SelfLink', 'Delete'];
			this.gridData = result.data;
		});
	}

	public PromptCreateDatabase() {
		this.form = this.formBuilder.group({
			DatabaseId: [],
		});
		this.dialogRef = this.dialog.open(this.createDatabaseDialog);
	}

	public CreateDatabase() {
		this.StartBusy();
		this.webapi.InvokePost('/api/core/databases', this.form.value, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.dialogRef.close();
			this.ViewDatabases();
		});
	}

	public PromptDeleteDatabase(id?: string) {
		this.form = this.formBuilder.group({
			DatabaseId: [id],
		});
		this.dialogRef = this.dialog.open(this.deleteDatabaseDialog);
	}

	public DeleteDatabase() {
		let id: string = this.form.value.DatabaseId;
		this.StartBusy();
		this.webapi.InvokeDelete(`/api/core/databases/${id}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			if (this.dialogRef) {
				this.dialogRef.close();
			}
			this.ViewDatabases();
		});
	}

	private StartBusy() {
		this.gridColumns = null;
		this.gridData = null;
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}
