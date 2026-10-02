import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../../environments/environment';
import { StaffMember } from '../../academic/data/academic.models';
import { StaffComponent } from './staff.component';

describe('StaffComponent', () => {
  const lecturer: StaffMember = {
    id: 'staff',
    staffNumber: 'L-1',
    displayName: 'Ama Mensah',
    email: 'ama@claimbase.test',
    employmentType: 'PartTime',
    biometricId: null,
    departments: [
      {
        departmentId: 'dept',
        departmentName: 'Computer Science',
        campusId: 'campus',
        campusName: 'Main',
        facultyId: 'faculty',
        facultyName: 'Science',
      },
    ],
    positions: [],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  function flushList(http: HttpTestingController): void {
    http.expectOne(`${environment.apiUrl}/api/staff?page=1&pageSize=20`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/position-titles`).flush({
      items: [],
      page: 1,
      pageSize: 100,
      totalCount: 0,
    });
  }

  it('lowercases the email and sends at least one department', () => {
    const fixture = TestBed.createComponent(StaffComponent);
    const http = TestBed.inject(HttpTestingController);
    flushList(http);

    fixture.componentInstance.startCreate();
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/campuses`).flush({
      items: [],
      page: 1,
      pageSize: 100,
      totalCount: 0,
    });
    fixture.componentInstance.form.setValue({
      staffNumber: 'L-2',
      displayName: 'Kofi Mensah',
      email: ' Kofi@ClaimBase.test ',
      employmentType: 'FullTime',
      biometricId: '',
      campusId: '',
      facultyId: '',
      departmentId: '',
    });
    fixture.componentInstance.draftDepartments.set([
      {
        departmentId: 'dept',
        departmentName: 'Computer Science',
        campusId: 'campus',
        campusName: 'Main',
        facultyId: 'faculty',
        facultyName: 'Science',
      },
    ]);
    fixture.componentInstance.submit();

    const create = http.expectOne(`${environment.apiUrl}/api/staff`);
    expect(create.request.body).toEqual({
      staffNumber: 'L-2',
      displayName: 'Kofi Mensah',
      email: 'kofi@claimbase.test',
      employmentType: 'FullTime',
      biometricId: null,
      departmentIds: ['dept'],
    });
    create.flush({ ...lecturer, id: 'created', email: 'kofi@claimbase.test' });
    http.verify();
  });

  it('does not offer to remove the last department', () => {
    const fixture = TestBed.createComponent(StaffComponent);
    const http = TestBed.inject(HttpTestingController);
    flushList(http);

    fixture.componentInstance.startEdit(lecturer);
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/campuses`).flush({
      items: [],
      page: 1,
      pageSize: 100,
      totalCount: 0,
    });
    fixture.detectChanges();

    const remove = [...fixture.nativeElement.querySelectorAll('button')].find((button) =>
      (button as HTMLButtonElement).textContent?.includes('Remove'),
    ) as HTMLButtonElement;
    expect(remove.disabled).toBe(true);
    http.verify();
  });
});
