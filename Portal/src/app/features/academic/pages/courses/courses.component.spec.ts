import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { CoursesComponent } from './courses.component';

describe('CoursesComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CoursesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('submits the course code in uppercase', () => {
    const fixture = TestBed.createComponent(CoursesComponent);
    const http = TestBed.inject(HttpTestingController);
    const qualification = { id: 'qual', name: 'Diploma' };
    const page = { items: [qualification], page: 1, pageSize: 20, totalCount: 1 };

    const requests = http.match((request) => request.url === `${environment.apiUrl}/api/qualifications`);
    expect(requests).toHaveLength(2);
    requests.forEach((request) => request.flush(page));
    http.expectOne(`${environment.apiUrl}/api/courses?page=1&pageSize=20`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });

    fixture.componentInstance.courseForm.setValue({
      code: ' cs101 ',
      name: 'Intro',
      qualificationId: 'qual',
    });
    fixture.componentInstance.submitCourse();

    const create = http.expectOne(`${environment.apiUrl}/api/courses`);
    expect(create.request.body).toEqual({ code: 'CS101', name: 'Intro', qualificationId: 'qual' });
    create.flush({
      id: 'course',
      code: 'CS101',
      name: 'Intro',
      qualificationId: 'qual',
      qualificationName: 'Diploma',
    });
    http.expectOne(`${environment.apiUrl}/api/courses?page=1&pageSize=20`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    http.verify();
  });

  it('keeps the edit banner on the original code while the field changes', () => {
    const fixture = TestBed.createComponent(CoursesComponent);
    const http = TestBed.inject(HttpTestingController);
    const qualification = { id: 'qual', name: 'Diploma' };
    const page = { items: [qualification], page: 1, pageSize: 20, totalCount: 1 };
    const requests = http.match((request) => request.url === `${environment.apiUrl}/api/qualifications`);
    requests.forEach((request) => request.flush(page));
    http.expectOne(`${environment.apiUrl}/api/courses?page=1&pageSize=20`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    fixture.componentInstance.edit({
      id: 'course',
      code: 'CS101',
      name: 'Intro',
      qualificationId: 'qual',
      qualificationName: 'Diploma',
    });
    fixture.detectChanges();
    fixture.componentInstance.courseForm.controls.code.setValue('MATH9');
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
    expect(status.textContent).toContain('Editing CS101.');
    expect(status.textContent).not.toContain('MATH9');
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('#course-code'));
    http.verify();
  });
});
