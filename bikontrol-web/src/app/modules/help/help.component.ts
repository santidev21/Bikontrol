import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Public help / FAQ page for customers (how to use Bikontrol). Content mirrors
 * the real flows: onboarding, motorcycles, maintenance, reminders, exports,
 * costs and account. Linked from the profile and the login/register footers.
 */
@Component({
  selector: 'app-help',
  imports: [RouterLink],
  templateUrl: './help.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HelpComponent {
  /** Support contact; replace with the definitive address. */
  readonly supportContact = '[COMPLETAR: contacto de soporte]';

  readonly faqs: readonly { question: string; answer: string }[] = [
    {
      question: '¿Bikontrol sirve para talleres?',
      answer:
        'No. Es una app para dueños de motos: cada cuenta gestiona sus propias motos y su historial.',
    },
    {
      question: '¿Necesito instalar algo?',
      answer:
        'No. Es una aplicación web (PWA): podés usarla desde el navegador y, si querés, “Agregar a la pantalla de inicio” para abrirla como una app.',
    },
    {
      question: '¿Qué es el “porcentaje de vida” de un mantenimiento?',
      answer:
        'Es cuánto le queda al intervalo antes de vencer: 100% recién hecho y 0% cuando ya toca (vencido).',
    },
    {
      question: '¿Qué pasa si me equivoco con el kilometraje?',
      answer:
        'Desde el resumen de la moto podés revertir el último kilometraje y volver a cargar el valor correcto.',
    },
    {
      question: '¿Puedo usar Bikontrol para varias motos?',
      answer:
        'Sí. Agregá todas las motos que quieras; mantenimientos y registros se guardan por moto.',
    },
    {
      question: '¿Cómo cancelo los recordatorios?',
      answer:
        'En Perfil podés desactivar los recordatorios por correo y, por dispositivo, las notificaciones push.',
    },
  ];
}
