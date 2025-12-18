import React, { useState, useEffect } from 'react'

// --- TYPY DANYCH ---
interface OccupiedDate {
  CheckInDate: string;
  CheckOutDate: string;
}

interface Room {
  Id: number;
  RoomNumber: string;
  Type: string;
  Capacity: number;
  PricePerNight: number;
  OccupiedDates: OccupiedDate[]; // Nowe pole z backendu
}

interface MyReservation {
  Id: string;
  RoomNumber: string;
  RoomType: string;
  CheckInDate: string;
  CheckOutDate: string;
  TotalPrice: number;
}

// Pobieranie URL API
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:7071/api';

function App() {
  // --- STATE ---
  const [view, setView] = useState<'book' | 'check' | 'schedule'>('book'); // Dodano 'schedule'
  
  const [rooms, setRooms] = useState<Room[]>([]);
  const [formData, setFormData] = useState({
    guestName: '',
    guestEmail: '',
    roomId: '',
    checkInDate: '',
    checkOutDate: ''
  });

  const [checkEmail, setCheckEmail] = useState('');
  const [myReservations, setMyReservations] = useState<MyReservation[]>([]);

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{type: 'success'|'error', text: string} | null>(null);

  // --- EFEKTY ---
  // Funkcja pobierająca pokoje (używana przy starcie i po rezerwacji, żeby odświeżyć grafik)
  const fetchRooms = () => {
    fetch(`${API_URL}/rooms`)
      .then(res => res.json())
      .then(data => setRooms(data))
      .catch(err => console.error("Błąd pobierania pokoi", err));
  };

  useEffect(() => {
    fetchRooms();
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
      const price = data.Price || data.price;
      setMessage({ type: 'success', text: `Sukces! ID: ${data.ReservationId}. Cena: ${price} PLN` });
      
      // Odśwież grafik po udanej rezerwacji!
      fetchRooms();

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

  // Helper do formatowania daty
  const formatDate = (dateString: string) => new Date(dateString).toLocaleDateString('pl-PL');

  return (
    <div style={{ display: 'flex', justifyContent: 'center', width: '100%', minHeight: '100vh' }}>
    <div className="container mt-4 mb-5" style={{ maxWidth: '900px' }}>
      
      <div className="text-center mb-4">
        <h2>🏨 Smart Hotel Assistant</h2>
      </div>

      {/* NAVBAR */}
      <ul className="nav nav-pills nav-fill mb-4 shadow-sm p-2 bg-white rounded">
        <li className="nav-item">
          <button className={`nav-link ${view === 'book' ? 'active' : ''}`} onClick={() => setView('book')}>
            ➕ Nowa Rezerwacja
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${view === 'check' ? 'active' : ''}`} onClick={() => setView('check')}>
            🔍 Moje Rezerwacje
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${view === 'schedule' ? 'active' : ''}`} onClick={() => setView('schedule')}>
            📅 Grafik / Dostępność
          </button>
        </li>
      </ul>

      <div className="card shadow">
        <div className="card-body">
          
          {/* WIDOK: REZERWACJA */}
          {view === 'book' && (
            <form onSubmit={handleBookSubmit}>
              <h4 className="mb-3">Zarezerwuj pobyt</h4>
              
              <div className="row">
                <div className="col-md-6 mb-3">
                  <label>Imię i Nazwisko</label>
                  <input required type="text" className="form-control" 
                    value={formData.guestName} 
                    onChange={e => setFormData({...formData, guestName: e.target.value})} />
                </div>
                <div className="col-md-6 mb-3">
                  <label>E-mail</label>
                  <input required type="email" className="form-control" 
                    value={formData.guestEmail} 
                    onChange={e => setFormData({...formData, guestEmail: e.target.value})} />
                </div>
              </div>

              <div className="mb-3">
                <label>Wybierz Pokój</label>
                <select required className="form-select" 
                  value={formData.roomId} 
                  onChange={e => setFormData({...formData, roomId: e.target.value})}>
                  <option value="">-- Wybierz z listy --</option>
                  {rooms.map(room => (
                    <option key={room.Id} value={room.Id}>
                      Pokój {room.RoomNumber} ({room.Type}) - {room.PricePerNight} PLN/noc
                    </option>
                  ))}
                </select>
              </div>

              <div className="row">
                <div className="col-md-6 mb-3">
                  <label>Od</label>
                  <input required type="date" className="form-control" 
                    value={formData.checkInDate} 
                    onChange={e => setFormData({...formData, checkInDate: e.target.value})} />
                </div>
                <div className="col-md-6 mb-3">
                  <label>Do</label>
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
              <h4 className="mb-3">Wyszukaj rezerwacje</h4>
              <form onSubmit={handleCheckSubmit} className="d-flex gap-2 mb-4">
                <input required type="email" className="form-control" placeholder="Podaj swój e-mail..."
                  value={checkEmail} onChange={e => setCheckEmail(e.target.value)} />
                <button type="submit" className="btn btn-secondary">Szukaj</button>
              </form>

              {myReservations.length > 0 && (
                <table className="table table-striped">
                  <thead>
                    <tr>
                      <th>Pokój</th>
                      <th>Termin</th>
                      <th>Koszt</th>
                    </tr>
                  </thead>
                  <tbody>
                    {myReservations.map(res => (
                      <tr key={res.Id}>
                        <td>{res.RoomNumber} <small className="text-muted">({res.RoomType})</small></td>
                        <td>{formatDate(res.CheckInDate)} ➝ {formatDate(res.CheckOutDate)}</td>
                        <td>{res.TotalPrice} PLN</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}

          {/* WIDOK: GRAFIK (NOWY) */}
          {view === 'schedule' && (
            <div>
               <h4 className="mb-4">Dostępność Pokoi</h4>
               <div className="row">
                 {rooms.map(room => (
                   <div className="col-md-6 mb-4" key={room.Id}>
                     <div className="card h-100 border-light shadow-sm">
                       <div className="card-header d-flex justify-content-between align-items-center">
                         <strong>🚪 Pokój {room.RoomNumber}</strong>
                         <span className="badge bg-secondary">{room.Type}</span>
                       </div>
                       <div className="card-body">
                         <p className="card-text mb-2">
                           <small>Cena: {room.PricePerNight} PLN | Pojemność: {room.Capacity} os.</small>
                         </p>
                         
                         <h6 className="mt-3 text-muted">Zajęte terminy:</h6>
                         {room.OccupiedDates && room.OccupiedDates.length > 0 ? (
                           <ul className="list-group list-group-flush">
                             {room.OccupiedDates.map((d, index) => (
                               <li key={index} className="list-group-item list-group-item-danger py-1">
                                 🔒 {formatDate(d.CheckInDate)} — {formatDate(d.CheckOutDate)}
                               </li>
                             ))}
                           </ul>
                         ) : (
                           <div className="alert alert-success py-2 mb-0">✨ Obecnie wolny (brak przyszłych rezerwacji)</div>
                         )}
                       </div>
                     </div>
                   </div>
                 ))}
               </div>
            </div>
          )}

          {/* KOMUNIKATY GLOBALNE */}
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