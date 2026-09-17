import { Routes } from '@angular/router';

const loadProjectSelectionPage = () =>
  import('./features/projects/project-selection-page/project-selection-page')
    .then(({ ProjectSelectionPage }) => ProjectSelectionPage);

const loadCategorySelectionPage = () =>
  import('./features/projects/category-selection-page/category-selection-page')
    .then(({ CategorySelectionPage }) => CategorySelectionPage);

const loadTemplatesPage = () =>
  import('./features/templates/templates-page/templates-page')
    .then(({ TemplatesPage }) => TemplatesPage);

const loadRoutesPage = () =>
  import('./features/routes/routes-page/routes-page')
    .then(({ RoutesPage }) => RoutesPage);

const loadSettingsPage = () =>
  import('./features/settings/settings-page/settings-page')
    .then(({ SettingsPage }) => SettingsPage);

const loadGeneralSettingsPage = () =>
  import('./features/settings/general-settings-page/general-settings-page')
    .then(({ GeneralSettingsPage }) => GeneralSettingsPage);

export const routes: Routes = [
  { path: 'projects', loadComponent: loadProjectSelectionPage },
  { path: 'projects/:projectId/categories', loadComponent: loadCategorySelectionPage },
  { path: 'templates/new', redirectTo: 'templates/create', pathMatch: 'full' },
  { path: 'templates/create', loadComponent: loadTemplatesPage },
  { path: 'templates/edit/:templateId', loadComponent: loadTemplatesPage },
  { path: 'templates', loadComponent: loadTemplatesPage },
  { path: 'rules/create', loadComponent: loadRoutesPage },
  { path: 'rules/edit/:templateId', loadComponent: loadRoutesPage },
  { path: 'rules', loadComponent: loadRoutesPage },
  { path: 'dictionaries', loadComponent: loadSettingsPage },
  { path: 'settings', loadComponent: loadGeneralSettingsPage },
  { path: '', pathMatch: 'full', redirectTo: 'projects' },
  { path: '**', redirectTo: 'projects' },
];
