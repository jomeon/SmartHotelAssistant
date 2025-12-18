import React, { useState, useEffect } from 'react'

// --- TYPY DANYCH ---
interface Room {
  Id: number;
  RoomNumber: string;
  Type: string;
  Capacity: number;
  PricePerNight: number;
}

interface MyReservation {
  Id: string;
  RoomNumber: string;
  RoomType: string;
  CheckInDate: string;
  CheckOutDate: string;
  TotalPrice: number;
}

// Pobieranie URL API (z env lub localhost)
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:7071/api';

function App() {
  // --- STATE ---
  const [view, setView] = useState<'book' | 'check'>('book'); // Nawigacja
  
  // Dane formularza rezerwacji
  const [rooms, setRooms] = useState<Room[]>([]);
  const [formData, setFormData] = useState({
    guestName: '',
    guestEmail: '', // Nowe pole
    roomId: '',     // Wybór z listy
    checkInDate: '',
    checkOutDate: ''
  });

  // Dane do sprawdzania rezerwacji
  const [checkEmail, setCheckEmail] = useState('');
  const [myReservations, setMyReservations] = useState<MyReservation[]>([]);

  // Statusy
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{type: 'success'|'error', text: string} | null>(null);

  // --- EFEKTY ---
  // Pobierz listę pokoi przy starcie
  useEffect(() => {
    fetch(`${API_URL}/rooms`)
      .then(res => res.json())
      .then(data => setRooms(data))
      .catch(err => console.error("Błąd pobierania pokoi", err));
  }, []);

  // --- OBSŁUGA FORMULARZA REZERWACJI ---
  const handleBookSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);

    const payload = {
      GuestName: formData.guestName,
      GuestEmail: formData.guestEmail,
      RoomId: parseInt(formData.roomId),
      CheckInDate: formData.checkInDate,
      CheckOutDate: formData.checkOutDate
    };

    try {
      const res = await fetch(`${API_URL}/reservation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.status === 409) {
        throw new Error("Ten pokój jest zajęty w wybranym terminie!");
      }
      if (!res.ok) throw new Error("Błąd rezerwacji.");

      const data = await res.json();
      setMessage({ type: 'success', text: `Sukces! ID: ${data.ReservationId}. Cena: ${data.Price} PLN` });
    } catch (err: any) {
      setMessage({ type: 'error', text: err.message });
    } finally {
      setLoading(false);
    }
  };

  // --- OBSŁUGA SPRAWDZANIA REZERWACJI ---
  const handleCheckSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/my-reservations/${checkEmail}`);
      if(res.ok) {
        const data = await res.json();
        setMyReservations(data);
        if(data.length === 0) setMessage({type: 'error', text: "Brak rezerwacji dla tego maila."});
        else setMessage(null);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  // --- RENDEROWANIE ---
  return (
    <div style={{ display: 'flex', justifyContent: 'center', width: '100%', minHeight: '100vh' }}>
    <div className="container mt-4 mx-auto" style={{ minWidth: '800px'}}>
      
      {/* NAVBAR */}
      <ul className="nav nav-pills nav-fill mb-4 shadow-sm p-2 bg-white rounded">
        <li className="nav-item">
          <button className={`nav-link ${view === 'book' ? 'active' : ''}`} onClick={() => setView('book')}>
            🏨 Zarezerwuj Pokój
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${view === 'check' ? 'active' : ''}`} onClick={() => setView('check')}>
            🔍 Moje Rezerwacje
          </button>
        </li>
      </ul>

      <div className="card shadow">
        <div className="card-body">
          
          {/* WIDOK: REZERWACJA */}
          {view === 'book' && (
            <form onSubmit={handleBookSubmit}>
              <h4 className="mb-3">Nowa Rezerwacja</h4>
              
              <div className="row">
                <div className="col-md-6 mb-3">
                  <label>Imię i Nazwisko</label>
                  <input required type="text" className="form-control" 
                    value={formData.guestName} 
                    onChange={e => setFormData({...formData, guestName: e.target.value})} />
                </div>
                <div className="col-md-6 mb-3">
                  <label>Adres E-mail (do powiadomień)</label>
                  <input required type="email" className="form-control" 
                    value={formData.guestEmail} 
                    onChange={e => setFormData({...formData, guestEmail: e.target.value})} />
                </div>
              </div>

              <div className="mb-3 ">
                <label>Wybierz Pokój</label>
                <select required className="form-select" 
                  value={formData.roomId} 
                  onChange={e => setFormData({...formData, roomId: e.target.value})}>
                  <option value="">-- Wybierz z listy --</option>
                  {rooms.map(room => (
                    <option key={room.Id} value={room.Id}>
                      Pokój {room.RoomNumber} ({room.Type}) - {room.Capacity} os. - {room.PricePerNight} PLN/noc
                    </option>
                  ))}
                </select>
              </div>

              <div className="row">
                <div className="col-md-6 mb-3">
                  <label>Data Od</label>
                  <input required type="date" className="form-control" 
                    value={formData.checkInDate} 
                    onChange={e => setFormData({...formData, checkInDate: e.target.value})} />
                </div>
                <div className="col-md-6 mb-3">
                  <label>Data Do</label>
                  <input required type="date" className="form-control" 
                    value={formData.checkOutDate} 
                    onChange={e => setFormData({...formData, checkOutDate: e.target.value})} />
                </div>
              </div>

              <button type="submit" className="btn btn-primary w-100" disabled={loading}>
                {loading ? 'Przetwarzanie...' : 'Zatwierdź Rezerwację'}
              </button>
            </form>
          )}

          {/* WIDOK: MOJE REZERWACJE */}
          {view === 'check' && (
            <div>
              <h4 className="mb-3">Sprawdź swoje rezerwacje</h4>
              <form onSubmit={handleCheckSubmit} className="d-flex gap-2 mb-4">
                <input required type="email" className="form-control" placeholder="Twój e-mail"
                  value={checkEmail} onChange={e => setCheckEmail(e.target.value)} />
                <button type="submit" className="btn btn-secondary">Szukaj</button>
              </form>

              {myReservations.length > 0 && (
                <table className="table table-striped table-hover">
                  <thead>
                    <tr>
                      <th>Pokój</th>
                      <th>Od</th>
                      <th>Do</th>
                      <th>Cena</th>
                    </tr>
                  </thead>
                  <tbody>
                    {myReservations.map(res => (
                      <tr key={res.Id}>
                        <td>{res.RoomNumber} <small className="text-muted">({res.RoomType})</small></td>
                        <td>{new Date(res.CheckInDate).toLocaleDateString()}</td>
                        <td>{new Date(res.CheckOutDate).toLocaleDateString()}</td>
                        <td>{res.TotalPrice} PLN</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}

          {/* KOMUNIKATY */}
          {message && (
            <div className={`alert mt-3 alert-${message.type === 'success' ? 'success' : 'danger'}`}>
              {message.text}
            </div>
          )}
        
        </div>
      </div>
    </div>
    </div>
  )
}

export default App