import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { ApiService } from '../../../api.service';
import { SmsProject } from '../project.models';
import { UserService } from '../../../user.service';

@Component({
  selector: 'app-project-selection-page',
  standalone: true,
  templateUrl: './project-selection-page.html',
  styleUrl: './project-selection-page.scss',
})
export class ProjectSelectionPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  userName = '';
  projects: SmsProject[] = [];
  projectsLoading = false;
  projectsError = '';

  constructor(public userService: UserService) {
  }
  ngOnInit(): void {
    this.loadProjects();
    this.userService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((user) => {
        if (user) {
          this.userName = user.fullName;
        }
      });
  }

  loadProjects(): void {
    this.projectsLoading = true;
    this.projectsError = '';

    this.apiService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.projectsLoading = false;
      },
      error: (error) => {
        this.projects = [];
        this.projectsLoading = false;
        this.projectsError = 'לא ניתן לטעון את הפרויקטים מהשרת.';
        console.error('Failed to load SMS projects', error);
      },
    });
  }

  enterProject(project: SmsProject): void {
    void this.router.navigate(['/projects', project.projectId, 'categories']);
  }
}
