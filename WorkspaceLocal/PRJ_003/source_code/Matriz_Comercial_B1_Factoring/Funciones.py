# Databricks notebook source
def sql_safe(query):
  try:
    print ("sql_safe: query -> " + query)
    return spark.sql(query)
  except Exception as e:
    raise Exception (f'{{"coderror":"20001", "msgerror":"Error grave procesado consulta -> {str(e)}"}}')

# COMMAND ----------

# MAGIC %md
# MAGIC Fecha vacía

# COMMAND ----------

def fecha_vacia(fechaProcesoX):
  try:
    if len(fechaProcesoX) == 0:
      dbutils.notebook.exit("{\"coderror\":28002, \"msgerror\":\"Falta ingresar parámetro Fecha: "+fechaProcesoX+"\"}")
  except ValueError:
    dbutils.notebook.exit("{\"coderror\":28002, \"msgerror\":\"Falta ingresar parámetro Fecha: "+fechaProcesoX+"\"}")

# COMMAND ----------

# MAGIC %md
# MAGIC Fecha futura

# COMMAND ----------

def fecha_futura(fechaProcesoX):
  try:
    if fechaProcesoX >= formatted_date:
      dbutils.notebook.exit("{\"coderror\":28000, \"msgerror\":\"Parámetro de entrada período no puede ser superior a fecha actual: "+fechaProcesoX+"\"}")
  except ValueError:
    dbutils.notebook.exit("{\"coderror\":28000, \"msgerror\":\"Parámetro de entrada período no puede ser superior a fecha actual: "+fechaProcesoX+"\"}")

# COMMAND ----------

# Valida fecha valida
from datetime import datetime

def esFecha(valor, nombre):
   res = bool(datetime.strptime(str(valor), "%Y%m%d"))
   if not res:
    raise Exception(f'{{"coderror": 24001, "msgerror": " parámetro: {nombre}", "descerror": " error es tipo fecha (YYYYMMDD) {nombre}" }}') 
   else:
    print(f"""Parametro {nombre} es de tipo fecha : {valor}""")

# COMMAND ----------

#Valida formato correcto fecha YYYYMMDD
def esFecvalida(valor, nombre):
    if  int(valor[:4]) < 1 :
        raise Exception(f'{{"coderror": 24001, "msgerror": " parámetro: {nombre}", "descerror": " error año ingresado incorrecto" }}') 
    elif  int(valor[4:6]) < 1 or int(valor[4:6]) > 12:
        raise Exception(f'{{"coderror": 24001, "msgerror": " parámetro: {nombre}", "descerror": " error mes ingresado incorrecto" }}') 
    elif  int(valor[6:]) < 1 or int(valor[6:]) > 31:
        raise Exception(f'{{"coderror": 24001, "msgerror": " parámetro: {nombre}", "descerror": " error día ingresado incorrecto" }}') 
    else:
        print("Fecha correcta!")  # esta sentencia no se ejecuta


# COMMAND ----------

#------------------------- Valida que la BD exista -------------------------
def valida_bd(nombrebdx, nombre):
  database = spark.sql(f"SHOW DATABASES LIKE '{nombrebdx}'").collect()
  print(f"""Parametro {nombre}: {nombrebdx} ---> Base de Datos encontrada""")
  if len(database) == 0:
    raise Exception("{\"coderror\":28009, \"msgerror\":\"Base de Datos no encontrada: "+str(nombrebdx)+"\"}")

# COMMAND ----------

#valida BD
def validaBD(nombrebdx):
  database = spark.sql(f"SHOW DATABASES LIKE '{nombrebdx}'").collect()

  if len(database) == 0:
    raise Exception("{\"coderror\":28009, \"msgerror\":\"Base de Datos no encontrada: "+str(nombrebdx)+"\"}")

# COMMAND ----------

#------------------------- Valida parametros no vengan vacíos -------------------------
#Librerias
import json
from datetime import datetime, date, time, timedelta
from dateutil.relativedelta import relativedelta

##Hora de inicio
hora_ini = datetime.now()

#Funcion Control Parametros Vacíos
def valida_param_vacio(valor, nombre):
  if (len(valor) == 0):
    raise Exception(f'{{"coderror": 24001, "msgerror": "Falta parámetro: {nombre}", "descerror": "Falta parámetro {nombre}" }}')
  else:
    print(f"""Parametro {nombre}: {valor}""")

# COMMAND ----------

#------------------------- Valida que la fecha sea valida -------------------------
def valida_fecha_valida(periodox, nombre):
  try:
    if len(periodox) == 10:
      periodo = datetime.strptime(periodox, '%Y-%m-%d')
      print(f"""Parametro {nombre}: {periodox} ---> Fecha valida""")
    elif len(periodox) == 8:
      periodo = datetime.strptime(periodox, '%Y%m%d')
      print(f"""Parametro {nombre}: {periodox} ---> Fecha valida""")
    elif len(periodox) == 6:
      periodo = datetime.strptime(periodox, '%Y%m')
      print(f"""Parametro {nombre}: {periodox} ---> Fecha valida""")
    else:
      raise Exception("{\"coderror\":28005, \"msgerror\":\"Largo de parámetro de entrada período inválido: "+periodox+"\"}")
  except ValueError:
    raise Exception("{\"coderror\":28006, \"msgerror\":\"Parámetro de entrada período inválido: "+periodox+"\"}")

# COMMAND ----------

#------------------------- Valida que la ruta exista -------------------------
def valida_ruta(valor, nombre):
  try:
    ruta = dbutils.fs.ls(f"{valor}")
    print(f"""Parametro {nombre}: {valor} ---> Ruta encontrada""")
  except ValueError:
      raise Exception("{\"coderror\":28010, \"msgerror\":\"Ruta no encontrada: "+str(valor)+"\"}")