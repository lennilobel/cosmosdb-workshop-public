import { Component, ViewChild, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './mongoDbApiMenu.component.html',
	styleUrls: ['../../app.component.css']
})
export class MongoDbApiMenuComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	@ViewChild('createTaskDialog') private createTaskDialog!: TemplateRef<any>;
	public busyIndex: number = 0;
	public document: any;
	public documents!: any[] | null;
	public documentColumns!: any[];
	public dialogRef!: MatDialogRef<any, any>;
	public form!: FormGroup;

	constructor(
		private dialog: MatDialog,
		private formBuilder: FormBuilder,
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public CreateTaskListCollection() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/mongodb/tasklist/create/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public PromptCreateTaskDocument() {
		this.form = this.formBuilder.group({
			Name: [],
			Category: [],
			DueDate: [],
			Tags: [],
		});
		this.dialogRef = this.dialog.open(this.createTaskDialog);
	}

	public CreateTaskDocument() {
		this.StartBusy();
		this.webapi.InvokePost(`/api/mongodb/task`, this.form.value, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.dialogRef.close();
			this.document = result.data;
		});
	}

	public ViewTaskListCollection() {
		this.StartBusy();
		this.webapi.InvokeGet(`/api/mongodb/tasklist/View/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documents = result.data;
			this.documentColumns = ['Id', 'Name', 'Category', 'DueDate', 'Tags'];
		});
	}

	public DeleteTaskListCollection() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/mongodb/tasklist/delete/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteTasksDatabase() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/mongodb/tasks/delete/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	private StartBusy() {
		this.documents = null;
		this.document = null;
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}
