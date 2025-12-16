import React, { useState } from 'react'

// Definicja typu danych formularza
interface ReservationForm {
  guestName: string;
  roomNumber: string;
  checkInDate: string;
  checkOutDate: string;
  totalPrice: number;
}

// Definicja tego, co zwraca Backend (dla bezpieczeństwa typów)
interface ReservationResponse {
  ReservationId: string;
  Status?: string;
}

function App() {
  // 1. Stan formularza z typowaniem
  const [formData, setFormData] = useState<ReservationForm>({
    guestName: '',
    roomNumber: '',
    checkInDate: '',
    checkOutDate: '',
    totalPrice: 500
  })

  // Stany interfejsu
  const [reservationId, setReservationId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState<boolean>(false)

  // 2. Obsługa zmian w polach (React.ChangeEvent to typ zdarzenia zmiany inputa)
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prevState => ({
      ...prevState,
      [name]: value
    }))
  }

  // 3. Wysłanie formularza
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setReservationId(null)

    // Przygotowanie payloadu zgodnie z oczekiwaniami C#/Backendu (PascalCase)
    const payload = {
      GuestName: formData.guestName,
      RoomNumber: formData.roomNumber,
      CheckInDate: formData.checkInDate,
      CheckOutDate: formData.checkOutDate,
      TotalPrice: Number(formData.totalPrice) // Upewniamy się, że to liczba
    }

    try {
      const response = await fetch('http://localhost:7071/api/reservation', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      })

      if (!response.ok) {
        throw new Error(`Błąd serwera: ${response.statusText}`)
      }

      // Rzutowanie odpowiedzi na nasz interfejs
      const data = await response.json() as ReservationResponse
      setReservationId(data.ReservationId)

    } catch (err) {
      console.error(err)
      if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Wystąpił nieznany błąd połączenia.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="container mt-5" style={{ maxWidth: '600px' }}>
      <div className="card shadow">
        <div className="card-header bg-primary text-white text-center">
          <h3>🏨 Smart Hotel Assistant</h3>
        </div>
        <div className="card-body">
          
          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label className="form-label">Imię i Nazwisko</label>
              <input
                type="text"
                className="form-control"
                name="guestName"
                value={formData.guestName}
                onChange={handleChange}
                required
                placeholder="np. Jan Kowalski"
              />
            </div>

            <div className="row">
              <div className="col-md-6 mb-3">
                <label className="form-label">Nr Pokoju</label>
                <input
                  type="text"
                  className="form-control"
                  name="roomNumber"
                  value={formData.roomNumber}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="col-md-6 mb-3">
                <label className="form-label">Cena (PLN)</label>
                <input
                  type="number"
                  className="form-control"
                  name="totalPrice"
                  value={formData.totalPrice}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="row">
              <div className="col-md-6 mb-3">
                <label className="form-label">Data od</label>
                <input
                  type="date"
                  className="form-control"
                  name="checkInDate"
                  value={formData.checkInDate}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="col-md-6 mb-3">
                <label className="form-label">Data do</label>
                <input
                  type="date"
                  className="form-control"
                  name="checkOutDate"
                  value={formData.checkOutDate}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <button type="submit" className="btn btn-primary w-100" disabled={loading}>
              {loading ? 'Przetwarzanie...' : 'Zarezerwuj Pokój'}
            </button>
          </form>

          {/* Wynik Sukcesu */}
          {reservationId && (
            <div className="alert alert-success mt-4 text-center">
              <h5>✅ Rezerwacja przyjęta!</h5>
              <p className="mb-0">ID Rezerwacji: <strong>{reservationId}</strong></p>
            </div>
          )}

          {/* Wynik Błędu */}
          {error && (
            <div className="alert alert-danger mt-4 text-center">
              ❌ {error}
            </div>
          )}

        </div>
      </div>
    </div>
  )
}

export default App