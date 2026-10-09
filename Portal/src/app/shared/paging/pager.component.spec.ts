import { TestBed } from '@angular/core/testing';

import { PagerComponent } from './pager.component';

describe('PagerComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PagerComponent],
    }).compileComponents();
  });

  it('does not invert the range when the page is past the last page', () => {
    const fixture = TestBed.createComponent(PagerComponent);
    fixture.componentRef.setInput('page', 3);
    fixture.componentRef.setInput('pageSize', 20);
    fixture.componentRef.setInput('totalCount', 25);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('25–25 of 25');
    expect(fixture.nativeElement.textContent).not.toContain('41');
  });
});
