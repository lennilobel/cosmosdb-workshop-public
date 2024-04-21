import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiContainers.component.html',
	styleUrls: [
		'../../app.component.css',
	]
})
export class CoreApiContainersComponent implements OnInit {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	@ViewChild('createContainerDialog') private createContainerDialog!: TemplateRef<any>;
	@ViewChild('deleteContainerDialog') private deleteContainerDialog!: TemplateRef<any>;
	public busyIndex: number = 0;
	public databaseSelectionForm!: FormGroup;
	public databases!: any;
	public dialogRef!: MatDialogRef<any, any>;
	public form!: FormGroup;
	public gridColumns!: any;
	public gridData!: any;

	constructor(
		private dialog: MatDialog,
		private formBuilder: FormBuilder,
		private webapi: WebApiService,
	) {
	}

	public ngOnInit() {
		this.databaseSelectionForm = this.formBuilder.group({
			DatabaseId: [],
		});
		this.GetDatabases();
	}

	private GetDatabases() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/databases', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.databases = result.data;
		});
	}

	public ViewContainers() {
		let id = this.databaseSelectionForm.value.DatabaseId;
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/containers/${id}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.gridColumns = ['ContainerId', 'PartitionKey', 'LastModified', 'Delete'];
			this.gridData = result.data;
		});

	}

	public PromptCreateContainer() {
		this.form = this.formBuilder.group({
			DatabaseId: [this.databaseSelectionForm.value.DatabaseId],
			ContainerId: [],
			PartitionKey: [],
			Throughput: [1000]
		});
		this.dialogRef = this.dialog.open(this.createContainerDialog);
	}

	public CreateContainer() {
		this.StartBusy();
		this.webapi.InvokePost(`/api/core/containers`, this.form.value, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.dialogRef.close();
			this.ViewContainers();
		});
	}

	public PromptDeleteContainer(id?: string) {
		this.form = this.formBuilder.group({
			ContainerId: [id],
		});
		this.dialogRef = this.dialog.open(this.deleteContainerDialog);
	}

	public DeleteContainer() {
		let did: string = this.databaseSelectionForm.value.DatabaseId;
		let cid: string = this.form.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeDelete(`/api/core/containers/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.dialogRef.close();
			this.ViewContainers();
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
