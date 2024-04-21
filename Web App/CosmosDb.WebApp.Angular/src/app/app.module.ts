// modules
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSortModule } from '@angular/material/sort';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// components
import { AppComponent } from './app.component';
import { CassandraApiMenuComponent } from './cassandraApi/cassandraApiMenu/cassandraApiMenu.component';
import { GremlinApiMenuComponent } from './gremlinApi/gremlinApiMenu/gremlinApiMenu.component';
import { GremlinApiAirportQueryComponent } from './gremlinApi/gremlinApiAirportQuery/gremlinApiAirportQuery.component';
import { HomeMenuComponent } from './home/homeMenu/homeMenu.component';
import { MessageBoxComponent } from './shared/directives/messageBox/messageBox.component';
import { MongoDbApiMenuComponent } from './mongoDbApi/mongoDbApiMenu/mongoDbApiMenu.component';
import { CoreApiConflictsComponent } from './coreApi/coreApiConflicts/coreApiConflicts.component';
import { CoreApiContainersComponent } from './coreApi/coreApiContainers/coreApiContainers.component';
import { CoreApiDatabasesComponent } from './coreApi/coreApiDatabases/coreApiDatabases.component';
import { CoreApiDocumentsComponent } from './coreApi/coreApiDocuments/coreApiDocuments.component';
import { CoreApiIndexingComponent } from './coreApi/coreApiIndexing/coreApiIndexing.component';
import { CoreApiMenuComponent } from './coreApi/coreApiMenu/coreApiMenu.component';
import { CoreApiSampleDataComponent } from './coreApi/coreApiSampleData/coreApiSampleData.component';
import { CoreApiSecurityComponent } from './coreApi/coreApiSecurity/coreApiSecurity.component';
import { CoreApiServerComponent } from './coreApi/coreApiServer/coreApiServer.component';
import { TableApiMenuComponent } from './tableApi/tableApiMenu/tableApiMenu.component';

// services & pipes
import { FormatterService } from './shared/services/formatter.service';
import { JsonPipe } from '@angular/common';
import { WebApiService } from './shared/services/webapi.service';

// routes
const routes: Routes = [
	{ path: '', redirectTo: '/homeMenu', pathMatch: 'full' },
	{ path: 'homeMenu', component: HomeMenuComponent },
	{ path: 'coreApiMenu', component: CoreApiMenuComponent },
	{ path: 'coreApiSampleData', component: CoreApiSampleDataComponent },
	{ path: 'coreApiDatabases', component: CoreApiDatabasesComponent },
	{ path: 'coreApiConflicts', component: CoreApiConflictsComponent },
	{ path: 'coreApiContainers', component: CoreApiContainersComponent },
	{ path: 'coreApiDocuments', component: CoreApiDocumentsComponent },
	{ path: 'coreApiIndexing', component: CoreApiIndexingComponent },
	{ path: 'coreApiSecurity', component: CoreApiSecurityComponent },
	{ path: 'coreApiServer', component: CoreApiServerComponent },
	{ path: 'gremlinApiMenu', component: GremlinApiMenuComponent },
	{ path: 'gremlinApiAirportQuery', component: GremlinApiAirportQueryComponent },
	{ path: 'tableApiMenu', component: TableApiMenuComponent },
	{ path: 'mongoDbApiMenu', component: MongoDbApiMenuComponent },
	{ path: 'cassandraApiMenu', component: CassandraApiMenuComponent },
];

@NgModule({
	// modules
	imports: [
		BrowserAnimationsModule,
		BrowserModule,
		FormsModule,
		HttpClientModule,
		MatButtonModule,
		MatDialogModule,
		MatExpansionModule,
		MatInputModule,
		MatProgressSpinnerModule,
		MatSelectModule,
		MatSortModule,
		MatTableModule,
		ReactiveFormsModule,
		RouterModule.forRoot(routes),
	],
	// components
	declarations: [
		AppComponent,
		GremlinApiMenuComponent,
		GremlinApiAirportQueryComponent,
		HomeMenuComponent,
		MessageBoxComponent,
		MongoDbApiMenuComponent,
		CoreApiConflictsComponent,
		CoreApiContainersComponent,
		CoreApiDatabasesComponent,
		CoreApiDocumentsComponent,
		CoreApiIndexingComponent,
		CoreApiMenuComponent,
		CoreApiSampleDataComponent,
		CoreApiSecurityComponent,
		CoreApiServerComponent,
		TableApiMenuComponent,
		CassandraApiMenuComponent,
	],
	// services
	providers: [
		FormatterService,
		JsonPipe,
		WebApiService,
	],
	bootstrap: [AppComponent]
})
export class AppModule { }
