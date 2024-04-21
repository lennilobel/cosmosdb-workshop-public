import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './cassandraApiMenu.component.html',
	styleUrls: ['../../app.component.css']
})
export class CassandraApiMenuComponent {

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
		this.webapi.InvokeGetText(`/api/cassandra/movies/create/`, (result: any) => {
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
		this.webapi.InvokeGetText(`/api/cassandra/movies/rows/create/`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public QueryAllMovies() {
		this.QueryMovies(`/api/cassandra/movies/rows/query/all`);
	}

	public QuerySciFiMovies() {
		this.QueryMovies(`/api/cassandra/movies/rows/query/scifi`);
	}

	public QueryStarWarsVI() {
		this.QueryMovies(`/api/cassandra/movies/rows/query/starWarsVI`);
	}

	private QueryMovies(url: string) {
		this.StartBusy();
		this.webapi.InvokeGet(url, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			console.log(result.data);
			this.documents = result.data;
			this.documentColumns = ['genre', 'title', 'year', 'length', 'description'];
		});
	}

	public DeleteMovieRow() {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/cassandra/movies/rows/delete/`, (result: any) => {
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
		this.webapi.InvokeGetText(`/api/cassandra/movies/delete/`, (result: any) => {
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
