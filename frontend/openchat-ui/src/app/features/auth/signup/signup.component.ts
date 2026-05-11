import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './signup.component.html'
})
export class SignupComponent {
  email = '';
  password = '';
  confirmPassword = '';
  error = signal('');
  loading = signal(false);

  constructor(private auth: AuthService, private router: Router) {
    if (auth.isAuthenticated) router.navigate(['/']);
  }

  get passwordStrength(): 'weak' | 'ok' | 'strong' | '' {
    if (!this.password) return '';
    if (this.password.length < 8) return 'weak';
    const hasLetter = /[a-zA-Z]/.test(this.password);
    const hasNumber = /\d/.test(this.password);
    const hasSpecial = /[^a-zA-Z0-9]/.test(this.password);
    if (hasLetter && hasNumber && hasSpecial && this.password.length >= 10) return 'strong';
    if (hasLetter && hasNumber) return 'ok';
    return 'weak';
  }

  submit(): void {
    this.error.set('');

    if (!this.email || !this.password || !this.confirmPassword) {
      this.error.set('Please fill in all fields.');
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }

    if (this.passwordStrength === 'weak') {
      this.error.set('Password must be at least 8 characters and include a letter and a number.');
      return;
    }

    this.loading.set(true);
    this.auth.signup({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.error.set(err.error?.error ?? 'Signup failed. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
