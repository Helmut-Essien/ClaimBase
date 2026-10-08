import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { FacultiesComponent } from './faculties.component';

describe('FacultiesComponent', () => {
  const base = environment.apiUrl;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FacultiesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('hides the department count while the selected faculty is still loading', () => {
    const fixture = TestBed.createComponent(FacultiesComponent);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne(`${base}/api/campuses?page=1&pageSize=100`).flush({
      items: [{ id: 'campus-1', name: 'Main Campus' }],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/faculties?page=1&pageSize=20`).flush({
      items: [
        { id: 'faculty-1', campusId: 'campus-1', campusName: 'Main Campus', name: 'Science' },
        { id: 'faculty-2', campusId: 'campus-1', campusName: 'Main Campus', name: 'Arts' },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2,
    });
    http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-1`).flush({
      items: [
        {
          id: 'dept-1',
          campusId: 'campus-1',
          campusName: 'Main Campus',
          facultyId: 'faculty-1',
          facultyName: 'Science',
          name: 'Physics',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('2 faculties');
    expect(fixture.nativeElement.textContent).toContain('1 department');

    fixture.componentInstance.selectFaculty('faculty-2');
    fixture.detectChanges();

    expect(fixture.componentInstance.departments()).toEqual([]);
    expect(fixture.nativeElement.textContent).not.toContain('0 departments');
    expect(fixture.nativeElement.textContent).toContain('Please wait…');

    http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-2`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('0 departments');
    expect(fixture.nativeElement.textContent).toContain('No departments in this faculty yet.');
    http.verify();
  });

  it('does not report zero departments when the department request fails', () => {
    const fixture = TestBed.createComponent(FacultiesComponent);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne(`${base}/api/campuses?page=1&pageSize=100`).flush({
      items: [{ id: 'campus-1', name: 'Main Campus' }],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/faculties?page=1&pageSize=20`).flush({
      items: [{ id: 'faculty-1', campusId: 'campus-1', campusName: 'Main Campus', name: 'Science' }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-1`).flush(
      { message: 'Departments could not be loaded.' },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Departments could not be loaded.');
    expect(fixture.nativeElement.textContent).not.toContain('0 departments');
    http.verify();
  });

  it('hides the department count when a later page fails', () => {
    const fixture = TestBed.createComponent(FacultiesComponent);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne(`${base}/api/campuses?page=1&pageSize=100`).flush({
      items: [{ id: 'campus-1', name: 'Main Campus' }],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/faculties?page=1&pageSize=20`).flush({
      items: [{ id: 'faculty-1', campusId: 'campus-1', campusName: 'Main Campus', name: 'Science' }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-1`).flush({
      items: [
        {
          id: 'dept-1',
          campusId: 'campus-1',
          campusName: 'Main Campus',
          facultyId: 'faculty-1',
          facultyName: 'Science',
          name: 'Physics',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 25,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('25 departments');

    fixture.componentInstance.loadDepartments(2);
    http.expectOne(`${base}/api/departments?page=2&pageSize=20&facultyId=faculty-1`).flush(
      { message: 'Departments could not be loaded.' },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Departments could not be loaded.');
    expect(fixture.nativeElement.textContent).not.toContain('25 departments');
    expect(fixture.nativeElement.textContent).not.toContain('Physics');
    http.verify();
  });

  it('waits in the department pane until a created faculty is on the list', () => {
    const fixture = TestBed.createComponent(FacultiesComponent);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne(`${base}/api/campuses?page=1&pageSize=100`).flush({
      items: [{ id: 'campus-1', name: 'Main Campus' }],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/faculties?page=1&pageSize=20`).flush({
      items: [{ id: 'faculty-1', campusId: 'campus-1', campusName: 'Main Campus', name: 'Science' }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-1`).flush({
      items: [
        {
          id: 'dept-1',
          campusId: 'campus-1',
          campusName: 'Main Campus',
          facultyId: 'faculty-1',
          facultyName: 'Science',
          name: 'Physics',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    fixture.componentInstance.facultyForm.controls.campusId.setValue('campus-1');
    fixture.componentInstance.facultyForm.controls.name.setValue('Arts');
    fixture.componentInstance.submitFaculty();
    const create = http.expectOne(`${base}/api/faculties`);
    create.flush({ id: 'faculty-2', campusId: 'campus-1', campusName: 'Main Campus', name: 'Arts' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Please wait…');
    expect(fixture.nativeElement.textContent).not.toContain('Physics');

    http.expectOne(`${base}/api/faculties?page=1&pageSize=20&campusId=campus-1`).flush({
      items: [{ id: 'faculty-2', campusId: 'campus-1', campusName: 'Main Campus', name: 'Arts' }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    const departments = http.expectOne(`${base}/api/departments?page=1&pageSize=20&facultyId=faculty-2`);
    departments.flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Arts');
    expect(fixture.nativeElement.textContent).not.toContain('Physics');
    http.verify();
  });
});
