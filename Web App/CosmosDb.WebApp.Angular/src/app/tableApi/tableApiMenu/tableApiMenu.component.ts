import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './tableApiMenu.component.html',
	styleUrls: ['../../app.component.css']
})
export class TableApiMenuComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public document: any;
	public documents!: any[] | null;
	public documentColumns!: any[];

	constructor(
		public formatter: FormatterService,
		private webapi: WebApiService,
	) {
	}

	public CreateMoviesTable() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/table/movies/create/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public CreateMovieRow() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/table/movies/rows/create/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public ViewMovieRows() {
		this.StartBusy();
		this.webapi.InvokeGet(`/api/table/movies/rows/view/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documents = result.data;
			this.documentColumns = ['PartitionKey', 'RowKey', 'Year', 'Length', 'Description'];
		});
	}

	public DeleteMovieRow() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/table/movies/rows/delete/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteMoviesTable() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/table/movies/delete/`, (result: any) => {
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
