import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiDocuments.component.html',
	styleUrls: [
		'./coreApiDocuments.component.css',
		'../../app.component.css',
	]
})
export class CoreApiDocumentsComponent implements OnInit {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public databases!: any[];
	public containers!: any[];
	public documentColumns!: any[];
	public containerSelectionForm!: FormGroup;
	public document: any;
	public documents!: any[] | null;
	private continuationToken: any;

	constructor(
		private dialog: MatDialog,
		private formBuilder: FormBuilder,
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public ngOnInit() {
		this.containerSelectionForm = this.formBuilder.group({
			DatabaseId: [],
			ContainerId: [],
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

	public GetContainers() {
		let id = this.containerSelectionForm.value.DatabaseId;
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/containers/${id}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.containers = result.data;
		});
	}

	public CreateDocument(demoType: string) {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/documents/create/${demoType}/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public QueryDocuments(demoType: string) {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/documents/query/${demoType}/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['DocumentId', 'Name', 'City' ];
			this.documents = result.data;
		});
	}

	public QueryDocumentsFirstPage() {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/documents/queryFirstPage/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['DocumentId', 'Name', 'City'];
			this.documents = result.data.data;
			this.document = JSON.parse(result.data.continuationToken);
			this.continuationToken = this.document;
		});
	}

	public QueryDocumentsNextPage() {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.busyIndex++;
		this.webapi.InvokePost(`/api/core/documents/queryNextPage/${did}/${cid}`, this.continuationToken, (result: any) => {
			this.busyIndex--;
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['DocumentId', 'Name', 'City'];
			this.documents = this.documents === null ? null : this.documents.concat(result.data.data);
			this.document = JSON.parse(result.data.continuationToken);
			this.continuationToken = this.document;
		});
	}

	public ReplaceDocuments() {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/documents/replace/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteDocuments() {
		let did: string = this.containerSelectionForm.value.DatabaseId;
		let cid: string = this.containerSelectionForm.value.ContainerId;
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/documents/delete/${did}/${cid}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	private StartBusy() {
		this.document = null;
		this.documents = null;
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}
